using cos.ViewModels;
using cos.Repositories;
using cos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace cos.Controllers
{
    
    public class CscController : Controller
    {
        private readonly IDataProtector _protector;
        private readonly CscRepository _cscRepository;
        private readonly IWebHostEnvironment _env;
        private readonly FileStoreService _fileStoreService;
        private bool _postToFileStore = true;

        public CscController(IDataProtectionProvider provider, IConfiguration configuration, IWebHostEnvironment env, FileStoreService fileStoreService)
        {
            _protector = provider.CreateProtector("DataProtector");
            _cscRepository = new CscRepository(configuration);
            _env = env;
            _fileStoreService = fileStoreService;
        }

        [Authorize(Roles = "csc_admin")]
        [HttpGet]
        public async Task<IActionResult> CtopUsers()
        {
            try
            {
                if (!IsCscAdmin())
                {
                    TempData["Error"] = "Access denied. CSC admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                var vm = await BuildPageViewModel();
                if (vm == null)
                {
                    TempData["Error"] = "Unable to load CSC admin context.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while loading the page: {ex.Message}";
                return RedirectToAction("AccessDenied", "Home");
            }
        }

        [Authorize(Roles = "csc_admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCtopUser(CtopUsersPageVM page)
        {
            try
            {
                if (!IsCscAdmin())
                {
                    TempData["Error"] = "Access denied. CSC admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                var context = await BuildPageViewModel(includeData: false);
                if (context == null || context.AdminMobile == null)
                {
                    TempData["Error"] = "Unable to load CSC admin context.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (!ModelState.IsValid)
                {
                    // rehydrate lists
                    await PopulateCircleAndSsaLists(page, page.NewCtop.circle_id);
                    page.AdminMobile = context.AdminMobile;
                    page.AdminCtop = context.AdminCtop;
                    page.Users = await _cscRepository.GetUsersByCtopAsync(context.AdminMobile);
                    return View("CtopUsers", page);
                }

                var newItem = page.NewCtop;
                if (newItem.circle_id == null)
                {
                    ModelState.AddModelError("NewCtop.circle_id", "Circle is required.");
                }
                if (newItem.ssa_id == null)
                {
                    ModelState.AddModelError("NewCtop.ssa_id", "SSA is required.");
                }
                if (!ModelState.IsValid)
                {
                    await PopulateCircleAndSsaLists(page, newItem.circle_id);
                    page.AdminMobile = context.AdminMobile;
                    page.AdminCtop = context.AdminCtop;
                    page.Users = await _cscRepository.GetUsersByCtopAsync(context.AdminMobile);
                    return View("CtopUsers", page);
                }

                var circle = await _cscRepository.GetCircleByIdAsync(newItem.circle_id!.Value);
                if (circle == null)
                {
                    ModelState.AddModelError("NewCtop.circle_id", "Invalid circle selected.");
                    await PopulateCircleAndSsaLists(page, newItem.circle_id);
                    page.AdminMobile = context.AdminMobile;
                    page.AdminCtop = context.AdminCtop;
                    page.Users = await _cscRepository.GetUsersByCtopAsync(context.AdminMobile);
                    return View("CtopUsers", page);
                }

                var ssa = await _cscRepository.GetSsaByIdAsync(newItem.ssa_id!.Value);
                if (ssa == null || ssa.circle_id != circle.id)
                {
                    ModelState.AddModelError("NewCtop.ssa_id", "Invalid SSA selected.");
                    await PopulateCircleAndSsaLists(page, newItem.circle_id);
                    page.AdminMobile = context.AdminMobile;
                    page.AdminCtop = context.AdminCtop;
                    page.Users = await _cscRepository.GetUsersByCtopAsync(context.AdminMobile);
                    return View("CtopUsers", page);
                }

                var posCode = CtopMaster.GeneratePosUniqueCode(newItem.aadhaar_no, newItem.aadhaar_issue_year, newItem.name);

                var entity = new CtopMaster
                {
                    username = newItem.contact_number,
                    ctopupno = context.AdminMobile,
                    name = newItem.name,
                    dealertype = newItem.dealertype,
                    ssa_code = ssa.ssa_code,
                    csccode = newItem.csccode,
                    circle_code = circle.circle_code,
                    attached_to = newItem.csccode, // attached_to is same as csccode
                    contact_number = newItem.contact_number,
                    pos_hno = newItem.pos_hno,
                    pos_street = newItem.pos_street,
                    pos_landmark = newItem.pos_landmark,
                    pos_locality = newItem.pos_locality,
                    pos_city = newItem.pos_city,
                    pos_district = ssa.ssa_name,
                    pos_state = circle.circle_name,
                    pos_pincode = newItem.pos_pincode,
                    created_date = DateTime.UtcNow,
                    pos_name_ss = newItem.pos_name_ss,
                    pos_owner_name = newItem.pos_owner_name,
                    pos_code = newItem.pos_code,
                    pos_ctop = context.AdminMobile,
                    circle_name = circle.circle_name,
                    pos_unique_code = posCode,
                    latitude = null,
                    longitude = null,
                    aadhaar_no = newItem.aadhaar_no,
                    zone_code = circle.zone_code,
                    ctop_type = string.IsNullOrEmpty(newItem.ctop_type) ? "INDIRECT" : newItem.ctop_type
                };

                // Get account ID for the insert
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie) || !long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    TempData["Error"] = "User session expired. Please login again.";
                    await PopulateCircleAndSsaLists(page, newItem.circle_id);
                    page.AdminMobile = context.AdminMobile;
                    page.AdminCtop = context.AdminCtop;
                    page.Users = await _cscRepository.GetUsersByCtopAsync(context.AdminMobile);
                    return View("CtopUsers", page);
                }

                var insertResult = await _cscRepository.InsertCtopAsync(entity, accountId);
                if (!insertResult.Success)
                {
                    TempData["Error"] = $"Failed to create CTOP user: {insertResult.ErrorMessage}";
                    await PopulateCircleAndSsaLists(page, newItem.circle_id);
                    page.AdminMobile = context.AdminMobile;
                    page.AdminCtop = context.AdminCtop;
                    page.Users = await _cscRepository.GetUsersByCtopAsync(context.AdminMobile);
                    return View("CtopUsers", page);
                }

                // Upload CSC staff POS registration documents linked to this username
                if (accountId > 0)
                {
                    var username = entity.username ?? string.Empty;

                    // Only attempt upload if file is provided (PAN is optional)
                    if (newItem.BaApprovalLetter != null && newItem.BaApprovalLetter.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.BaApprovalLetter, username, DocumentCategory.BA_APPROVAL_LETTER, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" BA Approval Letter upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.EmployeeIdCard != null && newItem.EmployeeIdCard.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.EmployeeIdCard, username, DocumentCategory.ID_CARD, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" Employee ID upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.AadhaarCard != null && newItem.AadhaarCard.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.AadhaarCard, username, DocumentCategory.AADHAR_CARD, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" Aadhaar upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.PanCard != null && newItem.PanCard.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.PanCard, username, DocumentCategory.PAN_CARD, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" PAN upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.Photo != null && newItem.Photo.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.Photo, username, DocumentCategory.PHOTO, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" Photo upload failed: {res.ErrorMessage}";
                        }
                    }
                }

                TempData["Success"] = "CTOP user created successfully.";
                return RedirectToAction(nameof(CtopUsers));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while processing the request: {ex.Message}";
                await PopulateCircleAndSsaLists(page, page.NewCtop.circle_id);
                page.AdminMobile = page.AdminMobile ?? string.Empty;
                page.AdminCtop = page.AdminCtop;
                return View("CtopUsers", page);
            }
        }

        private bool IsCscAdmin()
        {
            var roleCookie = HttpContext.Request.Cookies["Role"];
            var role = string.IsNullOrEmpty(roleCookie) ? string.Empty : _protector.Unprotect(roleCookie);
            return string.Equals(role, "csc_admin", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsBaAdmin()
        {
            var roleCookie = HttpContext.Request.Cookies["Role"];
            var role = string.IsNullOrEmpty(roleCookie) ? string.Empty : _protector.Unprotect(roleCookie);
            return string.Equals(role, "ba_admin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<CtopUsersPageVM?> BuildPageViewModel(bool includeData = true)
        {
            try
            {
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    return null;
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    return null;
                }

                var user = await _cscRepository.GetUserByAccountIdAsync(accountId);
                if (user == null || string.IsNullOrEmpty(user.mobile))
                {
                    return null;
                }

                var adminMobile = user.mobile;
                var adminCtop = await _cscRepository.GetCtopByUsernameAsync(adminMobile);
                if (adminCtop == null || !string.Equals(adminCtop.ctopupno, adminMobile, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var vm = new CtopUsersPageVM
                {
                    AdminMobile = adminMobile,
                    AdminCtop = adminCtop,
                    NewCtop = new CtopMasterCreateVM { ctop_type = "INDIRECT" }
                };

                if (includeData)
                {
                    var users = await _cscRepository.GetUsersByCtopAsync(adminMobile);
                    vm.Users = users;
                }

                var circles = await _cscRepository.GetCirclesAsync();
                vm.Circles = circles;
                vm.Ssas = new List<SsaOptionVM>();

                return vm;
            }
            catch (Exception ex)
            {
                // Log error if needed
                throw new Exception($"Error building page view model: {ex.Message}", ex);
            }
        }

        [Authorize(Roles = "csc_admin,ba_admin")]
        [HttpGet]
        public async Task<IActionResult> GetSsas(long circleId)
        {
            try
            {
                if (!IsCscAdmin() && !IsBaAdmin())
                {
                    return Unauthorized();
                }

                var ssas = await _cscRepository.GetSsasByCircleIdAsync(circleId);
                return Json(ssas);
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error loading SSAs: {ex.Message}" });
            }
        }

        [Authorize(Roles = "csc_admin,ba_admin")]
        [HttpGet]
        public async Task<IActionResult> GetCscCodesBySsa(string ssaCode)
        {
            try
            {
                if (!IsCscAdmin() && !IsBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ssaCode))
                {
                    return Json(new { codes = new List<CscCodeOptionVM>() });
                }

                var cscCodes = await _cscRepository.GetCscCodesBySsaAsync(ssaCode);
                return Json(new { codes = cscCodes });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Agents feature methods
        [Authorize(Roles = "ba_admin")]
        [HttpGet]
        public async Task<IActionResult> Agents()
        {
            try
            {
                if (!IsBaAdmin())
                {
                    TempData["Error"] = "Access denied. BA admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                var page = new AgentsPageVM
                {
                    Circles = await _cscRepository.GetCirclesAsync(),
                    Ssas = new List<SsaOptionVM>(),
                    NewAgent = new CtopMasterCreateVM { dealertype = "AGENT" }
                };

                return View(page);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading page: {ex.Message}";
                return RedirectToAction("AccessDenied", "Home");
            }
        }

        [Authorize(Roles = "ba_admin")]
        [HttpGet]
        public async Task<IActionResult> SearchRetailerCtop(string ctopupno)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    return Json(new { results = new List<CtopSearchResultVM>() });
                }

                var results = await _cscRepository.SearchRetailerCtopAsync(ctopupno);
                return Json(new { results });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "ba_admin")]
        [HttpGet]
        public async Task<IActionResult> GetRetailerDetails(string ctopupno)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    return Json(new { error = "CTOPUP number is required" });
                }

                var retailer = await _cscRepository.GetRetailerByCtopupnoAsync(ctopupno);
                if (retailer == null)
                {
                    return Json(new { error = "Retailer not found" });
                }

                return Json(new { retailer });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "ba_admin")]
        [HttpPost]
        [IgnoreAntiforgeryToken] // DataTables AJAX doesn't send anti-forgery token by default
        public async Task<IActionResult> GetAgentsByRetailer([FromBody] GetAgentsRequest request)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(request?.retailerCtopupno))
                {
                    return Json(new { data = new List<CtopMaster>(), recordsTotal = 0, recordsFiltered = 0 });
                }

                var agents = await _cscRepository.GetAgentsByRetailerCtopAsync(request.retailerCtopupno);
                var agentsList = agents.ToList();

                // Server-side filtering and pagination for DataTables
                var searchValue = request.search?.value?.ToLower() ?? "";
                var filteredData = agentsList.Where(a =>
                    string.IsNullOrEmpty(searchValue) ||
                    (a.name?.ToLower().Contains(searchValue) == true) ||
                    (a.username?.ToLower().Contains(searchValue) == true) ||
                    (a.contact_number?.ToLower().Contains(searchValue) == true)
                ).ToList();

                // Pagination
                var start = request.start ?? 0;
                var length = request.length ?? 10;
                var pagedData = filteredData.Skip(start).Take(length).ToList();

                return Json(new
                {
                    data = pagedData,
                    recordsTotal = agentsList.Count,
                    recordsFiltered = filteredData.Count,
                    draw = request.draw
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "ba_admin")]
        [HttpPost]
        //[ValidateAntiForgeryToken] // Commented out for AJAX submission
        public async Task<IActionResult> CreateAgent(AgentsPageVM page)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Access denied. BA admin role required.", errors = new List<string>() });
                    }
                    TempData["Error"] = "Access denied. BA admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (!ModelState.IsValid)
                {
                    if (IsAjaxRequest())
                    {
                        var errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();
                        
                        return Json(new
                        {
                            success = false,
                            message = "Please correct the validation errors.",
                            errors = errors
                        });
                    }
                    
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (page.NewAgent.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(page.NewAgent.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                var newItem = page.NewAgent;
                if (newItem.circle_id == null)
                {
                    ModelState.AddModelError("NewAgent.circle_id", "Circle is required.");
                }
                if (newItem.ssa_id == null)
                {
                    ModelState.AddModelError("NewAgent.ssa_id", "SSA is required.");
                }
                if (string.IsNullOrWhiteSpace(page.RetailerCtopupno))
                {
                    ModelState.AddModelError("", "Retailer CTOPUP number is required.");
                }
                if (!ModelState.IsValid)
                {
                    // Check if this is an AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                        Request.Headers["Content-Type"].ToString().Contains("multipart/form-data"))
                    {
                        var errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();
                        
                        return Json(new
                        {
                            success = false,
                            message = "Please correct the validation errors.",
                            errors = errors
                        });
                    }
                    
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (newItem.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(newItem.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                var circle = await _cscRepository.GetCircleByIdAsync(newItem.circle_id!.Value);
                if (circle == null)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Invalid circle selected.", errors = new List<string> { "Invalid circle selected." } });
                    }
                    ModelState.AddModelError("NewAgent.circle_id", "Invalid circle selected.");
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (newItem.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(newItem.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                var ssa = await _cscRepository.GetSsaByIdAsync(newItem.ssa_id!.Value);
                if (ssa == null || ssa.circle_id != circle.id)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Invalid SSA selected.", errors = new List<string> { "Invalid SSA selected." } });
                    }
                    ModelState.AddModelError("NewAgent.ssa_id", "Invalid SSA selected.");
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (newItem.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(newItem.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                // Verify retailer exists
                var retailer = await _cscRepository.GetRetailerByCtopupnoAsync(page.RetailerCtopupno!);
                if (retailer == null)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Retailer not found.", errors = new List<string> { "Retailer not found." } });
                    }
                    ModelState.AddModelError("", "Retailer not found.");
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (newItem.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(newItem.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                // Get account ID for the insert
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie) || !long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "User session expired. Please login again.", errors = new List<string> { "User session expired." } });
                    }
                    TempData["Error"] = "User session expired. Please login again.";
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (newItem.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(newItem.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                var posCode = CtopMaster.GeneratePosUniqueCode(newItem.aadhaar_no, newItem.aadhaar_issue_year, newItem.name);

                var entity = new CtopMaster
                {
                    username = newItem.contact_number,
                    ctopupno = page.RetailerCtopupno, // Agent's ctopupno is the retailer's ctopupno
                    name = newItem.name,
                    dealertype = "AGENT",
                    ssa_code = ssa.ssa_code,
                    csccode = newItem.csccode,
                    circle_code = circle.circle_code,
                    attached_to = newItem.csccode,
                    contact_number = newItem.contact_number,
                    pos_hno = newItem.pos_hno,
                    pos_street = newItem.pos_street,
                    pos_landmark = newItem.pos_landmark,
                    pos_locality = newItem.pos_locality,
                    pos_city = newItem.pos_city,
                    pos_district = ssa.ssa_name,
                    pos_state = circle.circle_name,
                    pos_pincode = newItem.pos_pincode,
                    created_date = DateTime.UtcNow,
                    pos_name_ss = newItem.pos_name_ss,
                    pos_owner_name = newItem.pos_owner_name,
                    pos_code = newItem.pos_code,
                    pos_ctop = page.RetailerCtopupno,
                    circle_name = circle.circle_name,
                    pos_unique_code = posCode,
                    latitude = null,
                    longitude = null,
                    aadhaar_no = newItem.aadhaar_no,
                    zone_code = circle.zone_code,
                    ctop_type = string.IsNullOrEmpty(newItem.ctop_type) ? "INDIRECT" : newItem.ctop_type
                };

                var insertResult = await _cscRepository.InsertCtopAsync(entity, accountId);
                if (!insertResult.Success)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Failed to create agent.", errors = new List<string> { insertResult.ErrorMessage ?? "Unknown error" } });
                    }
                    TempData["Error"] = $"Failed to create agent: {insertResult.ErrorMessage}";
                    page.Circles = await _cscRepository.GetCirclesAsync();
                    if (newItem.circle_id.HasValue)
                    {
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(newItem.circle_id.Value);
                    }
                    return View("Agents", page);
                }

                // Upload documents
                if (accountId > 0)
                {
                    var username = entity.username ?? string.Empty;

                    if (newItem.BaApprovalLetter != null && newItem.BaApprovalLetter.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.BaApprovalLetter, username, DocumentCategory.BA_APPROVAL_LETTER, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" BA Approval Letter upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.EmployeeIdCard != null && newItem.EmployeeIdCard.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.EmployeeIdCard, username, DocumentCategory.ID_CARD, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" Employee ID upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.AadhaarCard != null && newItem.AadhaarCard.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.AadhaarCard, username, DocumentCategory.AADHAR_CARD, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" Aadhaar Card upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.PanCard != null && newItem.PanCard.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.PanCard, username, DocumentCategory.PAN_CARD, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" PAN Card upload failed: {res.ErrorMessage}";
                        }
                    }

                    if (newItem.Photo != null && newItem.Photo.Length > 0)
                    {
                        var res = await SaveUserDocumentAsync(newItem.Photo, username, DocumentCategory.PHOTO, accountId);
                        if (!res.Success)
                        {
                            TempData["Error"] = (TempData["Error"] ?? string.Empty) + $" Photo upload failed: {res.ErrorMessage}";
                        }
                    }
                }

                // Check if this is an AJAX request
                if (IsAjaxRequest())
                {
                    // Return JSON for AJAX requests
                    var errors = new List<string>();
                    if (TempData["Error"] != null)
                    {
                        errors.Add(TempData["Error"].ToString() ?? "");
                    }
                    
                    return Json(new
                    {
                        success = string.IsNullOrEmpty(TempData["Error"]?.ToString()),
                        message = TempData["Success"]?.ToString() ?? "Agent created successfully!",
                        errors = errors,
                        retailerCtopupno = page.RetailerCtopupno
                    });
                }
                
                // For non-AJAX requests, use redirect
                TempData["Success"] = "Agent created successfully!";
                return RedirectToAction(nameof(Agents));
            }
            catch (Exception ex)
            {
                // Check if this is an AJAX request
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred while creating the agent.",
                        errors = new List<string> { ex.Message }
                    });
                }
                
                TempData["Error"] = $"An error occurred: {ex.Message}";
                page.Circles = await _cscRepository.GetCirclesAsync();
                if (page.NewAgent.circle_id.HasValue)
                {
                    page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(page.NewAgent.circle_id.Value);
                }
                return View("Agents", page);
            }
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers["Content-Type"].ToString().Contains("multipart/form-data");
        }

        private async Task PopulateCircleAndSsaLists(CtopUsersPageVM page, long? circleId)
        {
            try
            {
                var circles = await _cscRepository.GetCirclesAsync();
                page.Circles = circles;
                if (circleId.HasValue)
                {
                    var ssas = await _cscRepository.GetSsasByCircleIdAsync(circleId.Value);
                    page.Ssas = ssas;
                }
                else
                {
                    page.Ssas = new List<SsaOptionVM>();
                }
            }
            catch (Exception)
            {
                // Log error if needed
                page.Circles = new List<CircleOptionVM>();
                page.Ssas = new List<SsaOptionVM>();
            }
        }

        // Document Management Endpoints
        [Authorize(Roles = "csc_admin,ba_admin")]
        [HttpGet]
        public async Task<IActionResult> GetDocuments(string username)
        {
            try
            {
                if (!IsCscAdmin() && !IsBaAdmin())
                {
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Username is required");
                }

                var documents = await _cscRepository.GetDocumentsByUsernameAsync(username);
                return Json(documents);
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error loading documents: {ex.Message}" });
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> SaveUserDocumentAsync(
    IFormFile file,
    string username,
    string fileCategoryCode,
    long accountId)
        {
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file == null || file.Length == 0)
                return (false, "Invalid file");

            if (file.Length > maxFileSize)
                return (false, "File size exceeds maximum allowed size (10MB)");

            var uploadDate = DateTime.UtcNow;
            string storedFileName = Path.GetFileName(file.FileName);

            // 1️⃣ Get existing document & soft delete
            var existingDoc = await _cscRepository
                .GetDocumentByUsernameAndCategoryAsync(username, fileCategoryCode);

            if (existingDoc != null)
            {
                await _cscRepository.UpdateDocumentStatusAsync(
                    existingDoc.id,
                    "INACTIVE",
                    accountId);
            }

            // 2️⃣ Save locally first (IFormFile stream can only be read once)
            string? altDocumentPath = null;
            string? altFileName = null;

            try
            {
                var shortCode = DocumentCategory.GetCategoryShortCode(fileCategoryCode);
                var webRoot = _env.WebRootPath ?? string.Empty;

                altDocumentPath = Path.Combine("app_docs", username, shortCode);
                var physicalDir = Path.Combine(webRoot, altDocumentPath);

                if (!Directory.Exists(physicalDir))
                    Directory.CreateDirectory(physicalDir);

                altFileName = Path.GetFileName(file.FileName);
                var physicalPath = Path.Combine(physicalDir, altFileName);

                // Save file locally first
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 3️⃣ Try uploading to File Store (best effort) - use the saved file
                if (_postToFileStore)
                {
                    try
                    {
                        // Create a new IFormFile from the saved file for FileStore upload
                        using (var savedFileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read))
                        {
                            // Create a temporary IFormFile wrapper
                            var fileBytes = new byte[savedFileStream.Length];
                            await savedFileStream.ReadAsync(fileBytes, 0, (int)savedFileStream.Length);
                            
                            // Create a memory stream from the bytes
                            using (var memoryStream = new MemoryStream(fileBytes))
                            {
                                // Create a FormFile from memory stream
                                var formFile = new FormFile(memoryStream, 0, fileBytes.Length, file.Name, file.FileName)
                                {
                                    Headers = file.Headers,
                                    ContentType = file.ContentType
                                };

                                var uploadResponse = await _fileStoreService.UploadFileAsync(formFile, uploadDate);

                                if (!string.IsNullOrWhiteSpace(uploadResponse?.url))
                                {
                                    storedFileName = Path.GetFileName(new Uri(uploadResponse.url).LocalPath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail - local file is already saved
                        System.Diagnostics.Debug.WriteLine($"FileStore upload failed (local file saved): {ex.Message}");
                        // Disable further attempts if FileStore is down
                        _postToFileStore = false;
                    }
                }
            }
            catch (Exception ex)
            {
                // Local save failed - this is critical
                return (false, $"Failed to save file locally: {ex.Message}");
            }

            // 4️⃣ Insert DB record
            var newDoc = new CtopMasterDoc
            {
                username = username,
                document_path = storedFileName,
                file_name = file.FileName,
                file_category = DocumentCategory.GetCategoryName(fileCategoryCode),
                file_category_code = fileCategoryCode,
                record_status = "ACTIVE",
                created_by = accountId,
                updated_by = accountId,
                created_on = uploadDate,
                updated_on = uploadDate,
                alt_document_path = altDocumentPath,
                alt_file_name = altFileName
            };

            var insertResult = await _cscRepository.InsertDocumentAsync(newDoc);
            if (!insertResult.Success)
                return (false, insertResult.ErrorMessage);

            return (true, null);
        }


        [Authorize(Roles = "csc_admin")]
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 10485760)] // 10MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile file, string username, string fileCategoryCode)
        {
            try
            {
                if (!IsCscAdmin())
                {
                    return Unauthorized();
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fileCategoryCode))
                {
                    return BadRequest(new { error = "Username and file category are required" });
                }

                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    return Unauthorized();
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    return Unauthorized();
                }

                var result = await SaveUserDocumentAsync(file, username, fileCategoryCode, accountId);
                if (!result.Success)
                {
                    return StatusCode(500, new { error = result.ErrorMessage ?? "Error uploading document" });
                }

                return Json(new { success = true, message = "Document uploaded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error uploading document: {ex.Message}" });
            }
        }

        [Authorize(Roles = "csc_admin,ba_admin")]
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(long docId)
        {
            try
            {
                if (!IsCscAdmin() && !IsBaAdmin())
                {
                    return Unauthorized();
                }

                var doc = await _cscRepository.GetDocumentByIdAsync(docId);
                if (doc == null || doc.record_status != "ACTIVE")
                {
                    return NotFound();
                }

                // Retrieve file from File Store API
                if (!doc.created_on.HasValue)
                {
                    return BadRequest("Document upload date is missing");
                }

                byte[]? fileBytes = null;
                Exception? lastError = null;

                // Try main FileStore API first
                try
                {
                    fileBytes = await _fileStoreService.RetrieveFileAsync(doc.document_path, doc.created_on.Value);
                }
            catch (FileNotFoundException fnfEx)
            {
                lastError = fnfEx;
                    fileBytes = null;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    fileBytes = null;
                }

                // If API failed or file not found, try local storage fallback
                if (fileBytes == null && !string.IsNullOrEmpty(doc.alt_document_path) && !string.IsNullOrEmpty(doc.alt_file_name))
                {
                    try
                    {
                        var webRoot = _env.WebRootPath ?? string.Empty;
                        var physicalPath = Path.Combine(webRoot, doc.alt_document_path, doc.alt_file_name);
                        if (System.IO.File.Exists(physicalPath))
                        {
                            fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                }

                if (fileBytes == null)
                {
                    if (lastError is FileNotFoundException)
                    {
                        return NotFound("File not found in File Store or local storage");
                    }
                    if (lastError != null)
                    {
                        return StatusCode(500, $"Error retrieving file: {lastError.Message}");
                    }
                    return NotFound("File not available");
                }

                // Always force download on client side
                var contentType = "application/octet-stream";
                return File(fileBytes, contentType, doc.file_name);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error downloading document: {ex.Message}";
                return RedirectToAction(nameof(CtopUsers));
            }
        }

        [Authorize(Roles = "csc_admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument([FromBody] DeleteDocumentRequest request)
        {
            try
            {
                if (!IsCscAdmin())
                {
                    return Unauthorized();
                }

                if (request == null || request.docId <= 0)
                {
                    return BadRequest(new { error = "Invalid document ID" });
                }

                var doc = await _cscRepository.GetDocumentByIdAsync(request.docId);
                if (doc == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                // Get account ID for updated_by
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    return Unauthorized();
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    return Unauthorized();
                }

                // Note: File Store API doesn't provide delete endpoint
                // Files are soft-deleted by marking record_status as INACTIVE
                // Physical files remain in File Store but are not accessible through the application

                // Update document status to inactive
                var updateResult = await _cscRepository.UpdateDocumentStatusAsync(request.docId, "INACTIVE", accountId);
                if (!updateResult.Success)
                {
                    return StatusCode(500, new { error = $"Failed to delete document: {updateResult.ErrorMessage}" });
                }

                return Json(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error deleting document: {ex.Message}" });
            }
        }

        // Edit POS Unique Code - BA Admin and CSC Admin
        [Authorize(Roles = "ba_admin,csc_admin")]
        [HttpGet]
        public IActionResult EditPosUniqueCode()
        {
            try
            {
                if (!IsBaAdmin() && !IsCscAdmin())
                {
                    TempData["Error"] = "Access denied. BA admin or CSC admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("AccessDenied", "Home");
            }
        }

        [Authorize(Roles = "ba_admin,csc_admin")]
        [HttpGet]
        public async Task<IActionResult> SearchCtopForEdit(string ctopupno)
        {
            try
            {
                if (!IsBaAdmin() && !IsCscAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    return Json(new { error = "CTOPUP number is required" });
                }

                var ctop = await _cscRepository.GetCtopByCtopupnoAsync(ctopupno);
                if (ctop == null)
                {
                    return Json(new { error = "CTOP record not found" });
                }

                return Json(new { success = true, ctop });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "ba_admin,csc_admin")]
        [HttpPost]
        public IActionResult GeneratePosUniqueCode([FromBody] GeneratePosUniqueCodeRequest request)
        {
            try
            {
                if (!IsBaAdmin() && !IsCscAdmin())
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(request?.aadhaarNo) || 
                    string.IsNullOrWhiteSpace(request?.aadhaarIssueYear) || 
                    string.IsNullOrWhiteSpace(request?.aadhaarName))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                var posUniqueCode = CtopMaster.GeneratePosUniqueCode(
                    request.aadhaarNo, 
                    request.aadhaarIssueYear, 
                    request.aadhaarName);

                return Json(new { success = true, posUniqueCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error generating code: {ex.Message}" });
            }
        }

        [Authorize(Roles = "ba_admin,csc_admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePosUniqueCode(string ctopupno, string newPosUniqueCode)
        {
            try
            {
                if (!IsBaAdmin() && !IsCscAdmin())
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Access denied" });
                    }
                    TempData["Error"] = "Access denied. BA admin or CSC admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "CTOPUP number is required" });
                    }
                    TempData["Error"] = "CTOPUP number is required.";
                    return RedirectToAction(nameof(EditPosUniqueCode));
                }

                if (string.IsNullOrWhiteSpace(newPosUniqueCode))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "New POS unique code is required" });
                    }
                    TempData["Error"] = "New POS unique code is required.";
                    return RedirectToAction(nameof(EditPosUniqueCode));
                }

                // Get account ID of the person making the change
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "User session expired" });
                    }
                    TempData["Error"] = "User session expired.";
                    return RedirectToAction("Index", "Home");
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Invalid user session" });
                    }
                    TempData["Error"] = "Invalid user session.";
                    return RedirectToAction("Index", "Home");
                }

                // Update the pos_unique_code (this will backup the old row automatically)
                var result = await _cscRepository.UpdatePosUniqueCodeAsync(ctopupno, newPosUniqueCode, accountId);
                
                if (!result.Success)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    TempData["Error"] = result.ErrorMessage;
                    return RedirectToAction(nameof(EditPosUniqueCode));
                }

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "POS unique code updated successfully" });
                }

                TempData["Success"] = "POS unique code updated successfully.";
                return RedirectToAction(nameof(EditPosUniqueCode));
            }
            catch (Exception ex)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
                }
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(EditPosUniqueCode));
            }
        }
    }

    [Authorize(Roles = "csc_admin")]
    public class DeleteDocumentRequest
    {
        public long docId { get; set; }
    }

    public class GeneratePosUniqueCodeRequest
    {
        public string? aadhaarNo { get; set; }
        public string? aadhaarIssueYear { get; set; }
        public string? aadhaarName { get; set; }
    }
}



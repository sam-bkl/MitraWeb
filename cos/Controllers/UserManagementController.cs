using cos.ViewModels;
using cos.Repositories;
using cos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace cos.Controllers
{
    [Authorize(Roles = "circle_admin,ba_admin")]
    public class UserManagementController : Controller
    {
        private readonly IDataProtector _protector;
        private readonly CscRepository _cscRepository;
        private readonly AccountRepository _accountRepository;
        private readonly IWebHostEnvironment _env;
        private readonly FileStoreService _fileStoreService;
        private bool _postToFileStore = true;

        public UserManagementController(IDataProtectionProvider provider, IConfiguration configuration, IWebHostEnvironment env, FileStoreService fileStoreService)
        {
            _protector = provider.CreateProtector("DataProtector");
            _cscRepository = new CscRepository(configuration);
            _accountRepository = new AccountRepository(configuration);
            _env = env;
            _fileStoreService = fileStoreService;
        }

        [HttpGet]
        public async Task<IActionResult> CreateCscAdmin()
        {
            try
            {
                if (!IsCircleAdminOrBaAdmin())
                {
                    TempData["Error"] = "Access denied. Circle admin or BA admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                var vm = new CreateCscAdminPageVM
                {
                    Circles = await _cscRepository.GetCirclesAsync(),
                    Ssas = new List<SsaOptionVM>(),
                    NewUser = new CscAdminCreateVM()
                };

                ViewBag.IsCircleAdmin = IsCircleAdminOrBaAdmin();
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", "UserDash");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCtop(string ctopupno)
        {
            try
            {
                if (!IsCircleAdminOrBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    return Json(new { results = new List<CtopSearchResultVM>() });
                }

                var results = await _cscRepository.SearchCtopByCtopupnoAsync(ctopupno);
                return Json(new { results });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckAccount(string ctopupno)
        {
            try
            {
                if (!IsCircleAdminOrBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    return Json(new { error = "CTOPUP number is required" });
                }

                // Get CTOP details
                var ctop = await _cscRepository.GetCtopByCtopupnoAsync(ctopupno);
                if (ctop == null)
                {
                    return Json(new { error = "CTOP not found" });
                }

                // Check if account exists (username = ctopupno)
                var accountId = await _accountRepository.GetAccountIdByUsernameAsync(ctopupno);
                if (accountId.HasValue)
                {
                    // Account exists, get user details
                    var existingUser = await _accountRepository.GetExistingUserDetailsAsync(accountId.Value);
                    return Json(new
                    {
                        accountExists = true,
                        ctop = new { ctopupno = ctop.ctopupno, name = ctop.name, contact_number = ctop.contact_number },
                        user = existingUser
                    });
                }

                // Account doesn't exist
                return Json(new
                {
                    accountExists = false,
                    ctop = new { ctopupno = ctop.ctopupno, name = ctop.name, contact_number = ctop.contact_number }
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCscAdmin(CreateCscAdminPageVM page)
        {
            try
            {
                if (!IsCircleAdminOrBaAdmin())
                {
                    TempData["Error"] = "Access denied. Circle admin or BA admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Get logged in user's account ID
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    TempData["Error"] = "Unable to identify logged in user.";
                    return RedirectToAction("CreateCscAdmin");
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var createdByAccountId))
                {
                    TempData["Error"] = "Invalid user session.";
                    return RedirectToAction("CreateCscAdmin");
                }

                var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(createdByAccountId);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.mobile))
                {
                    TempData["Error"] = "Unable to get logged in user details.";
                    return RedirectToAction("CreateCscAdmin");
                }

                var createdByMobile = loggedInUser.mobile;

                if (!ModelState.IsValid)
                {
                    await PopulateCircleAndSsaLists(page);
                    return View(page);
                }

                var newUser = page.NewUser;

                // Validate circle and SSA
                if (!newUser.circle_id.HasValue)
                {
                    ModelState.AddModelError("NewUser.circle_id", "Circle is required.");
                }
                if (!newUser.ssa_id.HasValue)
                {
                    ModelState.AddModelError("NewUser.ssa_id", "SSA is required.");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateCircleAndSsaLists(page);
                    return View(page);
                }

                // Validate circle and SSA match
                var circle = await _cscRepository.GetCircleByIdAsync(newUser.circle_id!.Value);
                if (circle == null)
                {
                    ModelState.AddModelError("NewUser.circle_id", "Invalid circle selected.");
                    await PopulateCircleAndSsaLists(page);
                    return View(page);
                }

                var ssa = await _cscRepository.GetSsaByIdAsync(newUser.ssa_id!.Value);
                if (ssa == null || ssa.circle_id != circle.id)
                {
                    ModelState.AddModelError("NewUser.ssa_id", "Invalid SSA selected.");
                    await PopulateCircleAndSsaLists(page);
                    return View(page);
                }

                // Set circle_code and ssa_code from selections
                newUser.circle = circle.circle_code;
                if (string.IsNullOrEmpty(newUser.ssa_code))
                {
                    newUser.ssa_code = ssa.ssa_code;
                }

                // Create account and user
                var result = await _accountRepository.CreateCscAdminAccountAsync(newUser, createdByAccountId, createdByMobile, _cscRepository);
                if (!result.Success)
                {
                    TempData["Error"] = $"Failed to create CSC admin user: {result.ErrorMessage}";
                    await PopulateCircleAndSsaLists(page);
                    return View(page);
                }

                TempData["Success"] = "CSC admin user created successfully.";
                return RedirectToAction("CreateCscAdmin");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                await PopulateCircleAndSsaLists(page);
                return View(page);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSsas(long circleId)
        {
            try
            {
                if (!IsCircleAdminOrBaAdmin())
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

        private bool IsCircleAdmin()
        {
            var roleCookie = HttpContext.Request.Cookies["Role"];
            var role = string.IsNullOrEmpty(roleCookie) ? string.Empty : _protector.Unprotect(roleCookie);
            return string.Equals(role, "circle_admin", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCircleAdminOrBaAdmin()
        {
            var roleCookie = HttpContext.Request.Cookies["Role"];
            var role = string.IsNullOrEmpty(roleCookie) ? string.Empty : _protector.Unprotect(roleCookie);
            return string.Equals(role, "circle_admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "ba_admin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task PopulateCircleAndSsaLists(CreateCscAdminPageVM page)
        {
            try
            {
                page.Circles = await _cscRepository.GetCirclesAsync();
                if (page.NewUser.circle_id.HasValue)
                {
                    page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(page.NewUser.circle_id.Value);
                }
                else
                {
                    page.Ssas = new List<SsaOptionVM>();
                }
            }
            catch (Exception)
            {
                page.Circles = new List<CircleOptionVM>();
                page.Ssas = new List<SsaOptionVM>();
            }
        }

        // SSA Admin Management
        [HttpGet]
        public async Task<IActionResult> CreateSsaAdmin()
        {
            try
            {
                if (!IsCircleAdmin())
                {
                    TempData["Error"] = "Access denied. Circle admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Get logged-in circle_admin's circle
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    TempData["Error"] = "Unable to identify logged in user.";
                    return RedirectToAction("Index", "UserDash");
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    TempData["Error"] = "Invalid user session.";
                    return RedirectToAction("Index", "UserDash");
                }

                var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(accountId);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.circle))
                {
                    TempData["Error"] = "Unable to get logged in user's circle information.";
                    return RedirectToAction("Index", "UserDash");
                }

                var circleCode = loggedInUser.circle;

                // Get SSA admins for this circle
                var ssaAdmins = await _accountRepository.GetSsaAdminsByCircleAsync(circleCode);

                var vm = new CreateSsaAdminPageVM
                {
                    SsaAdmins = ssaAdmins,
                    Circles = await _cscRepository.GetCirclesAsync(),
                    Ssas = new List<SsaOptionVM>(),
                    NewUser = new SsaAdminCreateVM(),
                    CircleAdminCircle = circleCode
                };

                // Pre-select the circle_admin's circle
                var circle = (await _cscRepository.GetCirclesAsync()).FirstOrDefault(c => c.circle_code == circleCode);
                if (circle != null)
                {
                    vm.NewUser.circle_id = circle.id;
                    vm.NewUser.circle = circleCode;
                    vm.Ssas = await _cscRepository.GetSsasByCircleIdAsync(circle.id);
                }

                ViewBag.IsCircleAdmin = true;
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", "UserDash");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSsaAdmin(CreateSsaAdminPageVM page)
        {
            try
            {
                if (!IsCircleAdmin())
                {
                    TempData["Error"] = "Access denied. Circle admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Get logged in user's account ID
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    TempData["Error"] = "Unable to identify logged in user.";
                    return RedirectToAction("CreateSsaAdmin");
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var createdByAccountId))
                {
                    TempData["Error"] = "Invalid user session.";
                    return RedirectToAction("CreateSsaAdmin");
                }

                var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(createdByAccountId);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.mobile) || string.IsNullOrEmpty(loggedInUser.circle))
                {
                    TempData["Error"] = "Unable to get logged in user details.";
                    return RedirectToAction("CreateSsaAdmin");
                }

                var createdByMobile = loggedInUser.mobile;
                var circleAdminCircle = loggedInUser.circle;

                if (!ModelState.IsValid)
                {
                    await PopulateSsaAdminLists(page, circleAdminCircle);
                    return View(page);
                }

                var newUser = page.NewUser;

                // Validate circle and SSA
                if (string.IsNullOrEmpty(newUser.circle))
                {
                    ModelState.AddModelError("NewUser.circle_id", "Circle is required.");
                }
                if (string.IsNullOrEmpty(newUser.ssa_code))
                {
                    ModelState.AddModelError("NewUser.ssa_id", "SSA is required.");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateSsaAdminLists(page, circleAdminCircle);
                    return View(page);
                }

                /*/ Validate that the selected circle matches the circle_admin's circle
                var circle = await _cscRepository.GetCircleByIdAsync(newUser.circle_id!.Value);
                if (circle == null || circle.circle_code != circleAdminCircle)
                {
                    ModelState.AddModelError("NewUser.circle_id", "You can only create SSA admins for your own circle.");
                    await PopulateSsaAdminLists(page, circleAdminCircle);
                    return View(page);
                }

                var ssa = await _cscRepository.GetSsaByIdAsync(newUser.ssa_id!.Value);
                if (ssa == null || ssa.circle_id != circle.id)
                {
                    ModelState.AddModelError("NewUser.ssa_id", "Invalid SSA selected.");
                    await PopulateSsaAdminLists(page, circleAdminCircle);
                    return View(page);
                }*/

                // Set circle_code and ssa_code from selections
                //newUser.circle = circle.circle_code;
                //if (string.IsNullOrEmpty(newUser.ssa_code))
                //{
                //    newUser.ssa_code = ssa.ssa_code;
                //}

                // Check if account already exists
                var existingAccountId = await _accountRepository.GetAccountIdByUsernameAsync(newUser.mobile!);
                if (existingAccountId.HasValue)
                {
                    ModelState.AddModelError("NewUser.mobile", "An account with this mobile number already exists.");
                    await PopulateSsaAdminLists(page, circleAdminCircle);
                    return View(page);
                }

                // Create account and user
                var result = await _accountRepository.CreateSsaAdminAccountAsync(newUser, createdByAccountId, createdByMobile, _cscRepository);
                if (!result.Success)
                {
                    TempData["Error"] = $"Failed to create SSA admin user: {result.ErrorMessage}";
                    await PopulateSsaAdminLists(page, circleAdminCircle);
                    return View(page);
                }

                TempData["Success"] = "SSA admin user created successfully.";
                return RedirectToAction("CreateSsaAdmin");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                await PopulateSsaAdminLists(page, page.CircleAdminCircle ?? string.Empty);
                return View(page);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSsasForSsaAdmin(long circleId)
        {
            try
            {
                if (!IsCircleAdmin())
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

        private async Task PopulateSsaAdminLists(CreateSsaAdminPageVM page, string circleCode)
        {
            try
            {
                page.Circles = await _cscRepository.GetCirclesAsync();
                page.SsaAdmins = await _accountRepository.GetSsaAdminsByCircleAsync(circleCode);
                page.CircleAdminCircle = circleCode;

                if (page.NewUser.circle_id.HasValue)
                {
                    page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(page.NewUser.circle_id.Value);
                }
                else
                {
                    // Pre-select the circle_admin's circle
                    var circle = page.Circles.FirstOrDefault(c => c.circle_code == circleCode);
                    if (circle != null)
                    {
                        page.NewUser.circle_id = circle.id;
                        page.NewUser.circle = circleCode;
                        page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(circle.id);
                    }
                    else
                    {
                        page.Ssas = new List<SsaOptionVM>();
                    }
                }
            }
            catch (Exception)
            {
                page.Circles = new List<CircleOptionVM>();
                page.Ssas = new List<SsaOptionVM>();
                page.SsaAdmins = new List<SsaAdminListVM>();
            }
        }

        // Missing CSC Admin Onboarding
        [HttpGet]
        public async Task<IActionResult> MissingCSCAdminOnboard()
        {
            try
            {
                if (!IsBaAdmin())
                {
                    TempData["Error"] = "Access denied. BA admin role required.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Get logged-in user's context
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    TempData["Error"] = "Unable to identify logged in user.";
                    return RedirectToAction("Index", "UserDash");
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    TempData["Error"] = "Invalid user session.";
                    return RedirectToAction("Index", "UserDash");
                }

                var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(accountId);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.circle))
                {
                    TempData["Error"] = "Unable to get logged in user details.";
                    return RedirectToAction("Index", "UserDash");
                }

                // Get circle to retrieve zone_code
                var circle = (await _cscRepository.GetCirclesAsync()).FirstOrDefault(c => c.circle_code == loggedInUser.circle);
                if (circle == null)
                {
                    TempData["Error"] = "Unable to get circle information.";
                    return RedirectToAction("Index", "UserDash");
                }

                var vm = new MissingCscAdminOnboardPageVM
                {
                    Circles = await _cscRepository.GetCirclesAsync(),
                    Ssas = await _cscRepository.GetSsasByCircleIdAsync(circle.id),
                    ZoneCode = circle.zone_code,
                    CircleCode = loggedInUser.circle,
                    SsaCode = loggedInUser.ssa_code,
                    NewCtop = new CtopMasterCreateVM()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", "UserDash");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchMissingCscCtop(string ctopupno, string zoneCode)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(ctopupno) || string.IsNullOrWhiteSpace(zoneCode))
                {
                    return Json(new { results = new List<CtopSearchResultVM>() });
                }

                var results = await _cscRepository.SearchMissingCscCtopByZoneAsync(ctopupno, zoneCode);
                return Json(new { results });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetMissingCscCtopDetails([FromBody] GetCtopDetailsRequest request)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    return Json(new { error = "Access denied" });
                }

                if (string.IsNullOrWhiteSpace(request.ctopupno) || string.IsNullOrWhiteSpace(request.zoneCode))
                {
                    return Json(new { error = "CTOPUP number and zone code are required" });
                }

                var ctopDetails = await _cscRepository.GetMissingCscCtopDetailsByZoneAsync(request.ctopupno, request.zoneCode);
                if (ctopDetails == null)
                {
                    return Json(new { error = "CTOP not found" });
                }

                return Json(ctopDetails);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Note: GetCscCodesBySsa has been moved to CscController for csc_admin access
        // This method is kept for backward compatibility but should not be used
        [HttpGet]
        public async Task<IActionResult> GetCscCodesBySsa(string ssaCode)
        {
            try
            {
                if (!IsBaAdmin())
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

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMissingCscAdmin(MissingCscAdminOnboardPageVM page)
        {
            // Always use AJAX method for file upload preservation
            // Check if request wants JSON response (AJAX) or HTML (postback)
            var acceptHeader = Request.Headers["Accept"].ToString();
            var wantsJson = acceptHeader.Contains("application/json") || 
                           Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (wantsJson)
            {
                return await CreateMissingCscAdminAjax(page);
            }
            return await CreateMissingCscAdminPostback(page);
        }

        private async Task<IActionResult> CreateMissingCscAdminAjax(MissingCscAdminOnboardPageVM page)
        {
            var errors = new List<string>();
            
            try
            {
                // Log for debugging
                System.Diagnostics.Debug.WriteLine("CreateMissingCscAdminAjax called");
                
                if (!IsBaAdmin())
                {
                    return Json(new { success = false, error = "Access denied. BA admin role required." });
                }

                // Get logged-in user's context
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    return Json(new { success = false, error = "Unable to identify logged in user." });
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var createdByAccountId))
                {
                    return Json(new { success = false, error = "Invalid user session." });
                }

                var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(createdByAccountId);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.mobile) || string.IsNullOrEmpty(loggedInUser.circle))
                {
                    return Json(new { success = false, error = "Unable to get logged in user details." });
                }

                var createdByMobile = loggedInUser.mobile;
                var circle = (await _cscRepository.GetCirclesAsync()).FirstOrDefault(c => c.circle_code == loggedInUser.circle);
                if (circle == null)
                {
                    return Json(new { success = false, error = "Unable to get circle information." });
                }

                // Collect ModelState errors
                if (!ModelState.IsValid)
                {
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        errors.Add(modelError.ErrorMessage);
                    }
                }

                var newCtop = page.NewCtop;
                var ctopupno = Request.Form["ctopupno"].ToString();
                
                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    errors.Add("CTOPUP number is required.");
                }

                if (!newCtop.circle_id.HasValue)
                {
                    errors.Add("Circle is required.");
                }
                if (!newCtop.ssa_id.HasValue)
                {
                    errors.Add("SSA is required.");
                }

                if (errors.Count > 0)
                {
                    return Json(new { success = false, errors = errors });
                }

                var selectedCircle = await _cscRepository.GetCircleByIdAsync(newCtop.circle_id!.Value);
                if (selectedCircle == null)
                {
                    return Json(new { success = false, errors = new[] { "Invalid circle selected." } });
                }

                var ssa = await _cscRepository.GetSsaByIdAsync(newCtop.ssa_id!.Value);
                if (ssa == null || ssa.circle_id != selectedCircle.id)
                {
                    return Json(new { success = false, errors = new[] { "Invalid SSA selected." } });
                }

                var existingAccountId = await _accountRepository.GetAccountIdByUsernameAsync(ctopupno);
                if (existingAccountId.HasValue)
                {
                    return Json(new { success = false, errors = new[] { "An account with this CTOPUP number already exists." } });
                }

                var posCode = CtopMaster.GeneratePosUniqueCode(newCtop.aadhaar_no, newCtop.aadhaar_issue_year, newCtop.name);

                var ctopEntity = new CtopMaster
                {
                    username = newCtop.contact_number,
                    ctopupno = ctopupno,
                    name = newCtop.name,
                    dealertype = newCtop.dealertype ?? "CSR",
                    ssa_code = ssa.ssa_code,
                    csccode = newCtop.csccode,
                    circle_code = selectedCircle.circle_code,
                    attached_to = newCtop.csccode, // attached_to is same as csccode
                    contact_number = newCtop.contact_number,
                    pos_hno = newCtop.pos_hno,
                    pos_street = newCtop.pos_street,
                    pos_landmark = newCtop.pos_landmark,
                    pos_locality = newCtop.pos_locality,
                    pos_city = newCtop.pos_city,
                    pos_district = ssa.ssa_name,
                    pos_state = selectedCircle.circle_name,
                    pos_pincode = newCtop.pos_pincode,
                    created_date = DateTime.UtcNow,
                    pos_name_ss = newCtop.pos_name_ss,
                    pos_owner_name = newCtop.pos_owner_name,
                    pos_code = newCtop.pos_code,
                    pos_ctop = ctopupno,
                    circle_name = selectedCircle.circle_name,
                    pos_unique_code = posCode,
                    latitude = null,
                    longitude = null,
                    aadhaar_no = newCtop.aadhaar_no,
                    zone_code = selectedCircle.zone_code,
                    ctop_type = string.IsNullOrEmpty(newCtop.ctop_type) ? "INDIRECT" : newCtop.ctop_type
                };

                var insertResult = await _cscRepository.InsertCtopAsync(ctopEntity, createdByAccountId);
                if (!insertResult.Success)
                {
                    return Json(new { success = false, errors = new[] { $"Failed to create CTOP user: {insertResult.ErrorMessage}" } });
                }

                var userModel = new CscAdminCreateVM
                {
                    ctopupno = ctopupno,
                    staff_name = newCtop.name,
                    mobile = ctopupno,
                    email = !string.IsNullOrWhiteSpace(newCtop.contact_number) 
                        ? newCtop.contact_number + "@temp.com" 
                        : ctopupno + "@temp.com",
                    hrno = 0,
                    designation_code = newCtop.designation,
                    ssa_code = ssa.ssa_code,
                    circle = selectedCircle.circle_code,
                    circle_id = selectedCircle.id,
                    ssa_id = ssa.id
                };

                var accountResult = await _accountRepository.CreateCscAdminAccountAsync(userModel, createdByAccountId, createdByMobile, _cscRepository);
                if (!accountResult.Success)
                {
                    return Json(new { success = false, errors = new[] { $"Failed to create account: {accountResult.ErrorMessage}" } });
                }

                // Handle file uploads similar to CscController
                var username = ctopEntity.username ?? string.Empty;
                var uploadErrors = new List<string>();

                if (newCtop.BaApprovalLetter != null && newCtop.BaApprovalLetter.Length > 0)
                {
                    var res = await SaveUserDocumentAsync(newCtop.BaApprovalLetter, username, DocumentCategory.BA_APPROVAL_LETTER, createdByAccountId);
                    if (!res.Success)
                    {
                        uploadErrors.Add($"BA Approval Letter upload failed: {res.ErrorMessage}");
                    }
                }

                if (newCtop.EmployeeIdCard != null && newCtop.EmployeeIdCard.Length > 0)
                {
                    var res = await SaveUserDocumentAsync(newCtop.EmployeeIdCard, username, DocumentCategory.ID_CARD, createdByAccountId);
                    if (!res.Success)
                    {
                        uploadErrors.Add($"Employee ID upload failed: {res.ErrorMessage}");
                    }
                }

                if (newCtop.AadhaarCard != null && newCtop.AadhaarCard.Length > 0)
                {
                    var res = await SaveUserDocumentAsync(newCtop.AadhaarCard, username, DocumentCategory.AADHAR_CARD, createdByAccountId);
                    if (!res.Success)
                    {
                        uploadErrors.Add($"Aadhaar upload failed: {res.ErrorMessage}");
                    }
                }

                if (newCtop.PanCard != null && newCtop.PanCard.Length > 0)
                {
                    var res = await SaveUserDocumentAsync(newCtop.PanCard, username, DocumentCategory.PAN_CARD, createdByAccountId);
                    if (!res.Success)
                    {
                        uploadErrors.Add($"PAN upload failed: {res.ErrorMessage}");
                    }
                }

                if (newCtop.Photo != null && newCtop.Photo.Length > 0)
                {
                    var res = await SaveUserDocumentAsync(newCtop.Photo, username, DocumentCategory.PHOTO, createdByAccountId);
                    if (!res.Success)
                    {
                        uploadErrors.Add($"Photo upload failed: {res.ErrorMessage}");
                    }
                }

                if (uploadErrors.Count > 0)
                {
                    // Log errors but don't fail the entire operation
                    // Return success with warnings
                    return Json(new { success = true, message = "Missing CSC admin onboarded successfully, but some file uploads failed.", warnings = uploadErrors });
                }

                return Json(new { success = true, message = "Missing CSC admin onboarded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errors = new[] { $"An error occurred: {ex.Message}" } });
            }
        }

        private async Task<IActionResult> CreateMissingCscAdminPostback(MissingCscAdminOnboardPageVM page)
        {
            try
            {
                if (!IsBaAdmin())
                {
                    TempData["Error"] = "Access denied. BA admin role required.";
                    return RedirectToAction("MissingCSCAdminOnboard");
                }

                // Get logged-in user's context
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (string.IsNullOrEmpty(accountCookie))
                {
                    TempData["Error"] = "Unable to identify logged in user.";
                    return RedirectToAction("MissingCSCAdminOnboard");
                }

                if (!long.TryParse(_protector.Unprotect(accountCookie), out var createdByAccountId))
                {
                    TempData["Error"] = "Invalid user session.";
                    return RedirectToAction("MissingCSCAdminOnboard");
                }

                var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(createdByAccountId);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.mobile) || string.IsNullOrEmpty(loggedInUser.circle))
                {
                    TempData["Error"] = "Unable to get logged in user details.";
                    return RedirectToAction("MissingCSCAdminOnboard");
                }

                var createdByMobile = loggedInUser.mobile;
                var circle = (await _cscRepository.GetCirclesAsync()).FirstOrDefault(c => c.circle_code == loggedInUser.circle);
                if (circle == null)
                {
                    TempData["Error"] = "Unable to get circle information.";
                    return RedirectToAction("MissingCSCAdminOnboard");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    var ctopupnoFromForm = Request.Form["ctopupno"].ToString();
                    if (!string.IsNullOrWhiteSpace(ctopupnoFromForm))
                    {
                        ViewBag.Ctopupno = ctopupnoFromForm;
                    }
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                var newCtop = page.NewCtop;
                var ctopupno = Request.Form["ctopupno"].ToString();
                
                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    ModelState.AddModelError("", "CTOPUP number is required.");
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                if (!newCtop.circle_id.HasValue)
                {
                    ModelState.AddModelError("NewCtop.circle_id", "Circle is required.");
                }
                if (!newCtop.ssa_id.HasValue)
                {
                    ModelState.AddModelError("NewCtop.ssa_id", "SSA is required.");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.Ctopupno = ctopupno;
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                var selectedCircle = await _cscRepository.GetCircleByIdAsync(newCtop.circle_id!.Value);
                if (selectedCircle == null)
                {
                    ModelState.AddModelError("NewCtop.circle_id", "Invalid circle selected.");
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.Ctopupno = ctopupno;
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                var ssa = await _cscRepository.GetSsaByIdAsync(newCtop.ssa_id!.Value);
                if (ssa == null || ssa.circle_id != selectedCircle.id)
                {
                    ModelState.AddModelError("NewCtop.ssa_id", "Invalid SSA selected.");
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.Ctopupno = ctopupno;
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                var existingAccountId = await _accountRepository.GetAccountIdByUsernameAsync(ctopupno);
                if (existingAccountId.HasValue)
                {
                    ModelState.AddModelError("", "An account with this CTOPUP number already exists.");
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.Ctopupno = ctopupno;
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                var posCode = CtopMaster.GeneratePosUniqueCode(newCtop.aadhaar_no, newCtop.aadhaar_issue_year, newCtop.name);

                var ctopEntity = new CtopMaster
                {
                    username = newCtop.contact_number,
                    ctopupno = ctopupno,
                    name = newCtop.name,
                    dealertype = newCtop.dealertype ?? "CSR",
                    ssa_code = ssa.ssa_code,
                    csccode = newCtop.csccode,
                    circle_code = selectedCircle.circle_code,
                    attached_to = newCtop.csccode, // attached_to is same as csccode
                    contact_number = newCtop.contact_number,
                    pos_hno = newCtop.pos_hno,
                    pos_street = newCtop.pos_street,
                    pos_landmark = newCtop.pos_landmark,
                    pos_locality = newCtop.pos_locality,
                    pos_city = newCtop.pos_city,
                    pos_district = ssa.ssa_name,
                    pos_state = selectedCircle.circle_name,
                    pos_pincode = newCtop.pos_pincode,
                    created_date = DateTime.UtcNow,
                    pos_name_ss = newCtop.pos_name_ss,
                    pos_owner_name = newCtop.pos_owner_name,
                    pos_code = newCtop.pos_code,
                    pos_ctop = ctopupno,
                    circle_name = selectedCircle.circle_name,
                    pos_unique_code = posCode,
                    latitude = null,
                    longitude = null,
                    aadhaar_no = newCtop.aadhaar_no,
                    zone_code = selectedCircle.zone_code,
                    ctop_type = string.IsNullOrEmpty(newCtop.ctop_type) ? "INDIRECT" : newCtop.ctop_type
                };

                var insertResult = await _cscRepository.InsertCtopAsync(ctopEntity, createdByAccountId);
                if (!insertResult.Success)
                {
                    ModelState.AddModelError("", $"Failed to create CTOP user: {insertResult.ErrorMessage}");
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.Ctopupno = ctopupno;
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                var userModel = new CscAdminCreateVM
                {
                    ctopupno = ctopupno,
                    staff_name = newCtop.name,
                    mobile = ctopupno,
                    email = !string.IsNullOrWhiteSpace(newCtop.contact_number) 
                        ? newCtop.contact_number + "@temp.com" 
                        : ctopupno + "@temp.com",
                    hrno = 0,
                    designation_code = "CSC_ADMIN",
                    ssa_code = ssa.ssa_code,
                    circle = selectedCircle.circle_code,
                    circle_id = selectedCircle.id,
                    ssa_id = ssa.id
                };

                var accountResult = await _accountRepository.CreateCscAdminAccountAsync(userModel, createdByAccountId, createdByMobile, _cscRepository);
                if (!accountResult.Success)
                {
                    ModelState.AddModelError("", $"Failed to create account: {accountResult.ErrorMessage}");
                    await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                    ViewBag.Ctopupno = ctopupno;
                    ViewBag.ShowForm = true;
                    return View("MissingCSCAdminOnboard", page);
                }

                TempData["Success"] = "Missing CSC admin onboarded successfully.";
                return RedirectToAction("MissingCSCAdminOnboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                var accountCookie = HttpContext.Request.Cookies["Account"];
                if (!string.IsNullOrEmpty(accountCookie) && long.TryParse(_protector.Unprotect(accountCookie), out var accountId))
                {
                    var loggedInUser = await _cscRepository.GetUserByAccountIdAsync(accountId);
                    if (loggedInUser != null && !string.IsNullOrEmpty(loggedInUser.circle))
                    {
                        var circle = (await _cscRepository.GetCirclesAsync()).FirstOrDefault(c => c.circle_code == loggedInUser.circle);
                        if (circle != null)
                        {
                            await PopulateMissingCscAdminLists(page, circle.zone_code, loggedInUser.circle, loggedInUser.ssa_code);
                            var ctopupnoFromForm = Request.Form["ctopupno"].ToString();
                            if (!string.IsNullOrWhiteSpace(ctopupnoFromForm))
                            {
                                ViewBag.Ctopupno = ctopupnoFromForm;
                            }
                            ViewBag.ShowForm = true;
                            return View("MissingCSCAdminOnboard", page);
                        }
                    }
                }
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("MissingCSCAdminOnboard");
            }
        }

        private bool IsBaAdmin()
        {
            var roleCookie = HttpContext.Request.Cookies["Role"];
            var role = string.IsNullOrEmpty(roleCookie) ? string.Empty : _protector.Unprotect(roleCookie);
            return string.Equals(role, "ba_admin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task PopulateMissingCscAdminLists(MissingCscAdminOnboardPageVM page, string zoneCode, string circleCode, string? ssaCode)
        {
            try
            {
                page.Circles = await _cscRepository.GetCirclesAsync();
                page.ZoneCode = zoneCode;
                page.CircleCode = circleCode;
                page.SsaCode = ssaCode;

                var circle = page.Circles.FirstOrDefault(c => c.circle_code == circleCode);
                if (circle != null)
                {
                    page.Ssas = await _cscRepository.GetSsasByCircleIdAsync(circle.id);
                    if (page.NewCtop.circle_id == null)
                    {
                        page.NewCtop.circle_id = circle.id;
                    }
                }
                else
                {
                    page.Ssas = new List<SsaOptionVM>();
                }
            }
            catch (Exception)
            {
                page.Circles = new List<CircleOptionVM>();
                page.Ssas = new List<SsaOptionVM>();
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
            // We save locally first, then try FileStore from the saved file
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

                // Save file locally first (this consumes the stream)
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 3️⃣ Try uploading to File Store (best effort) - read from saved file
                if (_postToFileStore)
                {
                    try
                    {
                        // Read the saved file and create a FormFile for FileStore
                        using (var savedFileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read))
                        {
                            var fileBytes = new byte[savedFileStream.Length];
                            await savedFileStream.ReadAsync(fileBytes, 0, (int)savedFileStream.Length);
                            
                            // Create a FormFile from the saved file bytes
                            using (var memoryStream = new MemoryStream(fileBytes))
                            {
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
                file_category = DocumentCategory.GetCategoryName(fileCategoryCode),
                file_category_code = fileCategoryCode,
                file_name = storedFileName,
                alt_document_path = altDocumentPath,
                alt_file_name = altFileName,
                record_status = "ACTIVE",
                created_by = accountId,
                created_on = uploadDate
            };

            var insertResult = await _cscRepository.InsertDocumentAsync(newDoc);
            return insertResult.Success ? (true, null) : (false, insertResult.ErrorMessage ?? "Failed to save document record");
        }
    }

    public class GetCtopDetailsRequest
    {
        public string? ctopupno { get; set; }
        public string? zoneCode { get; set; }
    }
}


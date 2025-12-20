using cos.Repositories;
using cos.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using YourProject.Repositories.Interfaces;

namespace cos.Controllers
{
    public class UserDashController : Controller
    {
        private readonly UserDashRepository userDashRepository;
        //  private readonly SharedRepository sharedRepository;
        private readonly IDataProtector _protector;
        private readonly IOracleRepository oracleRepository; //added for oracle handling
        private readonly ILogger<UserDashController> _logger; //for logging

        public UserDashController(IConfiguration configuration, IDataProtectionProvider provider, ILogger<UserDashController> logger)
        {
            userDashRepository = new UserDashRepository(configuration);
            oracleRepository = new OracleRepository(configuration);
            _protector = provider.CreateProtector("DataProtector");
            _logger = logger;
        }
        public IActionResult Index()
        {

            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            if (!string.IsNullOrEmpty(_cookieRole))
            {
                ViewBag.UserRole = _protector.Unprotect(_cookieRole);
            }

            //code to check password expiry
            var _Pwdflag = HttpContext.Request.Cookies["Pwdflag"];
            var pwdflg = _protector.Unprotect(_Pwdflag);
            if (String.Equals(pwdflg, "1"))
            {
                TempData["PasswordMessage"] = "Password expired .Please change your password ";
                return RedirectToAction("Pwd_Change", "Users");
            }
            //code to check password expiry

            return View();
        }
        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
       // [Authorize(Roles = "customer")]
        public async Task<JsonResult> GetInventory()
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            var Customer = _protector.Unprotect(_cookieLoggedIn);
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);
            InventoryVM inventories = await userDashRepository.GetInventory(Customer, ViewBag.Circle,role,ssa);

            return Json(inventories);
        }

        //public IActionResult dtable()
        //{
        //    return View();
        //}

        [HttpPost]
       // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> GetkycSummary()
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<SimSummaryVM> simSummary = await userDashRepository.GetkycSummary(ViewBag.Circle, role, ssa);
            return Json(simSummary);
        }

        // pending kyc request view
        public IActionResult pendingkycrequests()
        {
         
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            if (!string.IsNullOrEmpty(_cookieRole))
            {
                ViewBag.UserRole = _protector.Unprotect(_cookieRole);
            }

            return View();
        }

        // pending kyc request view
        public IActionResult Swappendingkycrequests()
        {

            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            if (!string.IsNullOrEmpty(_cookieRole))
            {
                ViewBag.UserRole = _protector.Unprotect(_cookieRole);
            }

            return View();
        }


        //get details of pending requests

        [HttpPost]
        // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> GetkycDetails(string cafType)
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<kycVM> kycVMs = await userDashRepository.Getkycdetails(ViewBag.Circle, role, ssa, cafType);
            return Json(kycVMs);
        }


        //get details of pending sim swap requests
        [HttpPost]
        // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> GetkycDetailsSwap()
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<kycVM> kycVMs = await userDashRepository.GetkycdetailsSwap(ViewBag.Circle, role, ssa);
            return Json(kycVMs);
        }

        // pending kyc request status view
        public IActionResult kycrequestsstatus()
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            return View();
        }
        //get details of kyc request status
        [HttpPost]
        // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> GetkycstatusDetails()
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<kycstatusVM> kycstatusVMs = await userDashRepository.Getkycstatusdetails(ViewBag.Circle, role, ssa);
            return Json(kycstatusVMs);
        }


        // see later kyc request status view
        public IActionResult kycrequestlater()
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            return View();
        }
        //get details of kyc request status see later
        [HttpPost]
        // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> GetkycstatusSeelater()
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<kycstatusVM> kycstatusVMs = await userDashRepository.Getkycstatusseelater(ViewBag.Circle, role, ssa);
            return Json(kycstatusVMs);
        }


        // rejected kyc request status view
        public IActionResult kycrequestrej()
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            return View();
        }
        //get details of kyc request status see later
        [HttpPost]
        // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> Getkycstatusrejected()
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<kycstatusVM> kycstatusVMs = await userDashRepository.Getkycstatusrejected(ViewBag.Circle, role, ssa);
            return Json(kycstatusVMs);
        }


       
        [HttpGet]
        public async Task<IActionResult> CAFForm(string cafslno)
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);

            if (string.IsNullOrEmpty(cafslno))
                return BadRequest("cafslno is required");

            var model = await userDashRepository.GetCAFDataAsync(cafslno);

            if (model == null)
                return NotFound($"CAF data not found for slno: {cafslno}");

            // FILL LOCAL ADDRESS HERE
            model.FillLocalAddressIfEmpty();

            if (model.Photo != null && model.Photo.Length > 0)
            {
                model.PhotoBase64 = Convert.ToBase64String(model.Photo);
            }

            //get photos
            if (model.LivePhotoTime != null)
            {
                var date = model.LivePhotoTime.Value.ToString("yyyy-MM-dd");
                model.PhotoUrl = Url.Action("GetCAFPhoto", "UserDash", new { cafno = model.Caf_Serial_No, date });
            }




            return View(model);
        }


        //form for sim swap
        [HttpGet]
        public async Task<IActionResult> CAFSwapForm(string cafslno)
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);

            if (string.IsNullOrEmpty(cafslno))
                return BadRequest("cafslno is required");

            var model = await userDashRepository.GetCAFDataSwapAsync(cafslno);

            if (model == null)
                return NotFound($"CAF data not found for cafslno: {cafslno}");

            // FILL LOCAL ADDRESS HERE
            model.FillLocalAddressIfEmpty();

            if (model.Photo != null && model.Photo.Length > 0)
            {
                model.PhotoBase64 = Convert.ToBase64String(model.Photo);
            }

            //get photos
            if (model.LivePhotoTime != null)
            {
                var date = model.LivePhotoTime.Value.ToString("yyyy-MM-dd");
                model.PhotoUrl = Url.Action("GetCAFPhoto", "UserDash", new { cafno = model.Caf_Serial_No, date });
            }




            return View(model);
        }
        


        //newly addeded for handling error
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitCAF(CafModel model)
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookieuserid = HttpContext.Request.Cookies["SessionUser"];
            ViewBag.userid = _protector.Unprotect(_cookieuserid);

            if (model == null || string.IsNullOrEmpty(model.Gsmnumber))
                return BadRequest("Invalid model");

            // 🔐 STEP 1: LOCK CAF
            bool locked = await userDashRepository.LockCAFAsync(
                model.Gsmnumber,
                ViewBag.LoggedIn
            );

            if (!locked)
            {
                TempData["Error"] = "This CAF is already being processed by another user.";
                return RedirectToAction("pendingkycrequests");
            }


            // 🔁 STEP 1.5: FETCH IMSI FOR USIM TYPE = 2 (BEFORE ORACLE)
            if (model.usimtype == 2)
            {
                try
                {
                    string? imsi = await userDashRepository.GetImsiFromUsimApiAsync(model);

                    if (string.IsNullOrWhiteSpace(imsi))
                    {
                        throw new Exception("IMSI returned null/empty");
                    }

                    model.Imsi = imsi; // ✅ set IMSI in model
                }
                catch (Exception ex)
                {
                    // 🔓 UNLOCK CAF
                    await userDashRepository.ReleaseLockAsync(
                        model.Gsmnumber,
                        ViewBag.LoggedIn
                    );

                    _logger.LogError(
                        ex,
                        "IMSI fetch failed for GSM: {gsm}",
                        model.Gsmnumber
                    );

                    TempData["Error"] =
                        "Error fetching IMSI. Please contact IT cell.";

                    ModelState.AddModelError(
                        "",
                        "Error fetching IMSI. Please contact IT cell."
                    );

                    return View("CAFForm", model); // ⛔ STOP EVERYTHING
                }
            }



            try
            {
              
                // STEP 2: INSERT INTO ORACLE FIRST
                bool oracleInsert = await oracleRepository.InsertBcdFromCafAsync(
                    model,
                    ViewBag.LoggedIn
                );

                if (!oracleInsert)
                {
                    // 🔓 UNLOCK ONLY IF ORACLE FAILED
                    await userDashRepository.ReleaseLockAsync(
                        model.Gsmnumber,
                        ViewBag.LoggedIn
                    );

                    TempData["Error"] = "Billing hitting failed. CAF not verified.";
                    _logger.LogWarning(
                        "Oracle BCD insert failed for GSM: {gsm}",
                        model.Gsmnumber
                    );

                    ModelState.AddModelError("", "Oracle insert failed. CAF not verified.");
                    return View("CAFForm", model);
                }

                // ✅ STEP 3: UPDATE PG AFTER ORACLE SUCCESS
                bool ok = await userDashRepository.SaveCAFEditableFieldsAsync(
                    model,
                    ViewBag.LoggedIn,
                    ViewBag.userid

                );

                if (ok)
                {
                    TempData["Success"] = "CAF verified and successfully submitted.";
                    _logger.LogInformation(
                        "CAF verified successfully for GSM: {gsm}",
                        model.Gsmnumber
                    );

                    return RedirectToAction("pendingkycrequests");
                }
                else
                {
                    // 🚫 DO NOT UNLOCK HERE
                    // Oracle already committed — must stay locked

                    _logger.LogCritical(
                        "CRITICAL: Oracle insert succeeded but PG update failed for GSM: {gsm}",
                        model.Gsmnumber
                    );

                    TempData["Error"] =
                        "CAF sent to billing but verification update failed. Please contact admin.";

                    ModelState.AddModelError(
                        "",
                        "CAF already sent to billing. Admin intervention required."
                    );

                    return View("CAFForm", model);
                }
            }
            catch (Exception ex)
            {
                // 🔓 UNLOCK ONLY IF ORACLE DID NOT COMMIT
                await userDashRepository.ReleaseLockAsync(
                    model.Gsmnumber,
                    ViewBag.LoggedIn
                );

                _logger.LogError(ex, "SubmitCAF exception for GSM: {gsm}", model.Gsmnumber);
                TempData["Error"] = "Unexpected error occurred.";

                return View("CAFForm", model);
            }
        }

        //submit swap caf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSwapCAF(CafSwapModel model)
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);

            var _cookieuserid = HttpContext.Request.Cookies["SessionUser"];
            ViewBag.userid = _protector.Unprotect(_cookieuserid);

            if (model == null || string.IsNullOrEmpty(model.Gsmnumber))
                return BadRequest("Invalid model");

            // 🔐 STEP 1: LOCK CAF
            bool locked = await userDashRepository.LockCAFAsync(
                model.Gsmnumber,
                ViewBag.LoggedIn
            );

            if (!locked)
            {
                TempData["Error"] =
                    "This CAF is already being processed by another user.";
                return RedirectToAction("Swappendingkycrequests");
            }

            try
            {
                // 🔁 STEP 1.5: GET SIM SWAP CHARGE
                decimal? simSwapCharge =
                    await userDashRepository.GetSimSwapChargeAsync(model.circle_code);

                // Decide activation status ONLY for swap
                if (simSwapCharge.HasValue && simSwapCharge.Value > 0)
                {
                    model.Swap_Activation_Status = "IF";   // Charge Initiated
                }
                else
                {
                    model.Swap_Activation_Status = null;   // normal flow
                }

                // 🔁 STEP 2: INSERT DEDUCTION REQUEST (ONLY IF CHARGE EXISTS)
                if (simSwapCharge.HasValue && simSwapCharge.Value > 0)
                {
                    bool chargeInserted =
                        await oracleRepository.InsertSimSwapAmountDeductRequestAsync(
                            model,
                            simSwapCharge.Value,
                            ViewBag.LoggedIn
                        );

                    if (!chargeInserted)
                    {
                        await userDashRepository.ReleaseLockAsync(
                            model.Gsmnumber,
                            ViewBag.LoggedIn
                        );

                        TempData["Error"] =
                            "SIM swap charge initiation failed. CAF not submitted.";

                        return RedirectToAction("Swappendingkycrequests");
                    }
                }

                // 🔁 STEP 3: INSERT SWAP DATA INTO ORACLE (MANDATORY)
                bool oracleInsert =
                    await oracleRepository.InsertBcdFromCafSwapAsync(
                        model,
                        ViewBag.LoggedIn
                    );

                if (!oracleInsert)
                {
                    await userDashRepository.ReleaseLockAsync(
                        model.Gsmnumber,
                        ViewBag.LoggedIn
                    );

                    TempData["Error"] =
                        "Billing hitting failed. CAF not verified.";

                    _logger.LogWarning(
                        "Oracle swap insert failed for GSM: {gsm}",
                        model.Gsmnumber
                    );

                    return RedirectToAction("pendingkycrequests");
                }

                // ✅ STEP 4: UPDATE PG AFTER ORACLE SUCCESS
                bool pgUpdated =
                    await userDashRepository.SaveCAFEditableFieldsSwapAsync(
                        model,
                        ViewBag.LoggedIn,
                        ViewBag.userid
                    );

                if (!pgUpdated)
                {
                    // 🚫 DO NOT UNLOCK — ORACLE COMMITTED
                    _logger.LogCritical(
                        "CRITICAL: Oracle insert succeeded but PG update failed for GSM: {gsm}",
                        model.Gsmnumber
                    );

                    TempData["Error"] =
                        "CAF sent to billing but verification update failed. Please contact admin.";

                    return View("CAFSwapForm", model);
                }

                // 🎉 SUCCESS
                TempData["Success"] =
                    "SIM swap CAF successfully submitted.";

                _logger.LogInformation(
                    "SIM swap CAF verified successfully for GSM: {gsm}",
                    model.Gsmnumber
                );

                return RedirectToAction("Swappendingkycrequests");
            }
            catch (Exception ex)
            {
                // 🔓 UNLOCK ONLY IF ORACLE DID NOT COMMIT
                await userDashRepository.ReleaseLockAsync(
                    model.Gsmnumber,
                    ViewBag.LoggedIn
                );

                _logger.LogError(
                    ex,
                    "SubmitSwapCAF exception for GSM: {gsm}",
                    model.Gsmnumber
                );

                TempData["Error"] =
                    "Unexpected error occurred. Please try again.";

                return View("CAFSwapForm", model);
            }
        }



        [HttpPost]
        public async Task<IActionResult> RejectCAF([FromBody] RejectModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Gsmnumber))
                return BadRequest("Invalid data");

            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookieuserid = HttpContext.Request.Cookies["SessionUser"];
            ViewBag.userid = _protector.Unprotect(_cookieuserid);

            bool result = await userDashRepository.RejectCAFAsync(model.Gsmnumber, model.Reason, ViewBag.LoggedIn, ViewBag.userid);

            if (result)
                return Ok();

            return StatusCode(500, "Failed to reject CAF");
        }

        //see later

        [HttpPost]
        public async Task<IActionResult> SeeLaterCAF([FromBody] SeelaterModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Gsmnumber))
                return BadRequest("Invalid data");

            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookieuserid = HttpContext.Request.Cookies["SessionUser"];
            ViewBag.userid = _protector.Unprotect(_cookieuserid);

            bool result = await userDashRepository.SeelaterCAFAsync(model.Gsmnumber, model.remark, ViewBag.LoggedIn, ViewBag.userid);

            if (result)
                return Ok();

            return StatusCode(500, "Failed to reject CAF");
        }


        //[HttpPost]
        //public async Task<IActionResult> UpdateCAFFlag([FromBody] FlagUpdateModel model)
        //{
        //    if (model == null || string.IsNullOrEmpty(model.Gsmnumber))
        //        return BadRequest("Invalid data");

        //    var cookie = HttpContext.Request.Cookies["LoggedIn"];
        //    string loggedin = _protector.Unprotect(cookie);

        //    bool result = await userDashRepository.UpdateCafFlagAsync(
        //        model.Gsmnumber,
        //        model.Reason,
        //        model.Flag,
        //        loggedin
        //    );

        //    return result ? Ok() : StatusCode(500, "Failed to update CAF");
        //}


        //[HttpPost]
        //public async Task<IActionResult> UpdateCAFFlag([FromBody] FlagUpdateModel model)
        //{
        //    if (model == null || string.IsNullOrEmpty(model.Gsmnumber))
        //        return BadRequest("Invalid data");

        //    var cookie = HttpContext.Request.Cookies["LoggedIn"];
        //    if (string.IsNullOrEmpty(cookie))
        //        return Unauthorized("Session expired");

        //    string loggedin = _protector.Unprotect(cookie);

        //    bool result = await userDashRepository.UpdateCafFlagAsync(
        //        model.Gsmnumber,
        //        model.Reason,
        //        model.Flag,
        //        loggedin
        //    );

        //    if (!result)
        //    {
        //        // CAF already processed OR locked by someone else
        //        return Conflict(new
        //        {
        //            message = "CAF is already processed or being handled by another user."
        //        });
        //    }

        //    return Ok(new
        //    {
        //        message = model.Flag == "Y"
        //            ? "CAF approved successfully."
        //            : "CAF rejected successfully."
        //    });
        //}



        //method for fetching photo
        [HttpGet]
        public async Task<IActionResult> GetCAFPhoto(string cafno, string date)
        {
            try
            {
                if (string.IsNullOrEmpty(cafno) || string.IsNullOrEmpty(date))
                    return NotFound();

                // date expected: YYYY-MM-DD
                var parts = date.Split('-');
                string year = parts[0];
                string month = parts[1];
                string day = parts[2];

                string remoteUrl =
                    $"http://10.201.222.68:9801/cos_images/{year}/{month}/{day}/{cafno}.jpg";

                // Disable proxy completely
                var handler = new HttpClientHandler
                {
                    Proxy = null,
                    UseProxy = false,
                    AutomaticDecompression = DecompressionMethods.None
                };

                using (var http = new HttpClient(handler))
                {
                    http.Timeout = TimeSpan.FromSeconds(60);

                    var response = await http.GetAsync(remoteUrl);

                    if (!response.IsSuccessStatusCode)
                        return NotFound();

                    var bytes = await response.Content.ReadAsByteArrayAsync();

                    return File(bytes, "image/jpeg");
                }
            }
            catch
            {
                return NotFound();
            }
        }




    }
}

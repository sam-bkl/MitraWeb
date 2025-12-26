using cos.Models;
using cos.Repositories;
using cos.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;


namespace cos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LoginRepository loginRepository;
        private readonly IDataProtector _protector;
        private readonly cos.Services.CaptchaService _captchaService;
        private readonly cos.ApiCallers.SmsApiCaller _smsApiCaller;
        private readonly CscRepository _cscRepository;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IDataProtectionProvider provider, cos.Services.CaptchaService captchaService, cos.ApiCallers.SmsApiCaller smsApiCaller)
        {
            _logger = logger;
            loginRepository = new LoginRepository(configuration);
            _protector = provider.CreateProtector("DataProtector");
            _captchaService = captchaService;
            _smsApiCaller = smsApiCaller;
            _cscRepository = new CscRepository(configuration);
        }

        public IActionResult Index()
        {
            ViewBag.Error = "";
            var model = new LoginVM { otp_sent = false };
            return View(model);
        }

        public IActionResult CaptchaImage()
        {
            var (imageData, captchaText) = _captchaService.GenerateCaptchaImage();
            HttpContext.Session.SetString("CaptchaCode", captchaText);
            return File(imageData, "image/png");
        }

        /*   [HttpPost] 
           [ValidateAntiForgeryToken]
          public async Task<IActionResult> Login(LoginVM postData)
           {
               ViewBag.Error = "";
               AccountVM account = new AccountVM();
               if (ModelState.IsValid)
               {
                   try
                   {
                       account = await loginRepository.authenticate(postData);
                       if (account != null)
                       {
                           //
                       }
                       else
                       {
                           ViewBag.Error = "Login failed! These credentials do not match our record";
                           return View();
                       }
                   }
                   catch (Exception err)
                   {
                       ViewBag.Error = err.Message;
                   }
                   return RedirectToAction("Index", "UserDash");

               }
               else
               {
                   ViewBag.Error = "Login failed! These credentials do not match our record";
                   return View();
               }

           } */




        [Route("Home/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[ValidateDNTCaptcha(
        //    ErrorMessage = "Please Enter Valid Captcha",
        //    CaptchaGeneratorLanguage = Language.English,
        //    CaptchaGeneratorDisplayMode = DisplayMode.SumOfTwoNumbers)]
        public async Task<IActionResult> Login(LoginVM postData)
        {
            _logger.LogInformation("Login POST called. User authenticated: {IsAuthenticated}, Username: {Username}, OTP sent: {OtpSent}", 
                User.Identity?.IsAuthenticated ?? false, 
                postData.username ?? "null", 
                postData.otp_sent);

            // If user is already authenticated, redirect to appropriate dashboard immediately
            // This prevents any OTP sending or processing
            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("Login POST called for already authenticated user. Redirecting to dashboard.");
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(role))
                {
                    return RedirectToRoleBasedPage(role);
                }
                return RedirectToAction("Index", "UserDash");
            }

            // Additional check: If there's no username/password and user is not in OTP flow, 
            // this might be an accidental POST - redirect to Index GET
            if (string.IsNullOrWhiteSpace(postData.username) && string.IsNullOrWhiteSpace(postData.password) && !postData.otp_sent)
            {
                _logger.LogWarning("Login POST called without credentials and not in OTP flow. Redirecting to Index.");
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "";
            postData.otp_sent = postData.otp_sent || false;

            if (!postData.otp_sent)
            {
                // Step 1: Validate CAPTCHA and credentials
                // Additional safety check: Ensure we have username and password
                if (string.IsNullOrWhiteSpace(postData.username) || string.IsNullOrWhiteSpace(postData.password))
                {
                    _logger.LogWarning("Login POST called without username or password. Redirecting to Index.");
                    return RedirectToAction("Index", "Home");
                }

                var captchaInSession = HttpContext.Session.GetString("CaptchaCode");
                if (captchaInSession == null || postData.captcha_input?.ToUpperInvariant() != captchaInSession.ToUpperInvariant())
                {
                    ViewBag.Error = "Incorrect CAPTCHA.";
                    postData.otp_sent = false;
                    return View("Index", postData);
                }
                HttpContext.Session.Remove("CaptchaCode");

                if (!ModelState.IsValid)
                {
                    postData.otp_sent = false;
                    return View("Index", postData);
                }

                try
                {
                    AccountVM account = await loginRepository.Authenticate(postData);

                    if (account == null || account.id <= 0)
                    {
                        ViewBag.Error = "Login failed! These credentials do not match our record";
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }

                    if (account.is_verified != "VERIFIED")
                    {
                        ViewBag.Error = "Login failed! Your account has not yet been verified. Please contact your system administrator.";
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }

                    // Check if user role requires OTP or can skip it
                    bool requiresOtp = account.role_name != "cc_admin" && account.role_name != "reports_view";

                    if (!requiresOtp)
                    {
                        // For cc_admin and reports_view, skip OTP and complete login directly
                        return await CompleteLoginAsync(account, postData);
                    }

                    // Generate and send OTP
                    var otp = GenerateOtp();

                    // Store in session
                    HttpContext.Session.SetString("OTP", otp);
                    HttpContext.Session.SetString("PreAuthAccountId", account.id.ToString());
                    HttpContext.Session.SetString("PreAuthUsername", account.user_name ?? "");
                    HttpContext.Session.SetString("PreAuthMobile", account.mobile.ToString());
                    HttpContext.Session.SetString("OTPTime", DateTime.UtcNow.ToString("O")); // ISO 8601 format for JavaScript parsing
                    HttpContext.Session.SetInt32("OTPTryCount", 0);

                    // Log OTP entry to database
                    var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? GetClientIp();
                    var lgchk = new LgchkVM
                    {
                        mobile = account.mobile,
                        uid = account.user_name,
                        id = account.id
                    };
                    await loginRepository.OtpLogEntry(lgchk, remoteIpAddress, otp);

                    // Get zone from circle if available
                    string? zone = null;
                    if (!string.IsNullOrWhiteSpace(account.circle))
                    {
                        var circles = await _cscRepository.GetCirclesAsync();
                        var circle = circles.FirstOrDefault(c => c.circle_code == account.circle);
                        zone = circle?.zone_code;
                    }

                    // Send OTP via SMS
                    string smsResult = await _smsApiCaller.SendOtpSms(account.mobile.ToString(), otp, zone, account.circle, account.ssa_code);
                    JObject jsonObject = JObject.Parse(smsResult);
                    string? messageId = jsonObject["Message_Id"]?.Value<string>();
                    string? error = jsonObject["Error"]?.Value<string>();

                    if (string.IsNullOrEmpty(messageId))
                    {
                        ViewBag.Error = error ?? "Failed to send OTP. Please try again.";
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }

                    // OTP sent successfully - pass OTP time to view for accurate countdown
                    ViewBag.OtpTime = HttpContext.Session.GetString("OTPTime");
                    postData.otp_sent = true;
                    return View("Index", postData);
                }
                catch (Exception err)
                {
                    ViewBag.Error = err.Message;
                    postData.otp_sent = false;
                    return View("Index", postData);
                }
            }
            else
            {
                // Step 2: Validate OTP
                var expectedOtp = HttpContext.Session.GetString("OTP");
                var preAuthAccountId = HttpContext.Session.GetString("PreAuthAccountId");
                var otpTimeStr = HttpContext.Session.GetString("OTPTime");
                var tryCount = HttpContext.Session.GetInt32("OTPTryCount") ?? 0;

                // Basic server-side checks
                if (string.IsNullOrEmpty(preAuthAccountId))
                {
                    ViewBag.Error = "Session expired. Please login again.";
                    postData.otp_sent = false;
                    return View("Index", postData);
                }

                // Limit attempts
                if (tryCount >= 5)
                {
                    ClearOtpSession();
                    ViewBag.Error = "Too many incorrect OTP attempts. Please login again.";
                    postData.otp_sent = false;
                    return View("Index", postData);
                }

                if (expectedOtp != postData.otp)
                {
                    HttpContext.Session.SetInt32("OTPTryCount", tryCount + 1);
                    ViewBag.Error = "Invalid OTP.";
                    postData.otp_sent = true;
                    return View("Index", postData);
                }

                // Check expiry
                if (DateTime.TryParse(otpTimeStr, out DateTime otpTime))
                {
                    if ((DateTime.UtcNow - otpTime).TotalMinutes > 5)
                    {
                        ClearOtpSession();
                        ViewBag.Error = "OTP expired. Please login again.";
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }
                }

                // OTP validated - proceed with authentication
                try
                {
                    // Get account information by ID from session
                    AccountVM? account = null;
                    
                    if (long.TryParse(preAuthAccountId, out long accountId))
                    {
                        account = await loginRepository.GetAccountByIdAsync(accountId);
                    }

                    // Validate account retrieval
                    if (account == null || account.id <= 0)
                    {
                        _logger.LogWarning($"Account retrieval failed for account ID: {preAuthAccountId}");
                        ViewBag.Error = "Session expired. Please login again.";
                        ClearOtpSession();
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }

                    // Verify account is still active and verified
                    if (account.is_verified != "VERIFIED")
                    {
                        _logger.LogWarning($"Account verification check failed for account ID: {account.id}");
                        ViewBag.Error = "Login failed! Your account has not yet been verified. Please contact your system administrator.";
                        ClearOtpSession();
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }

                    // Additional security check: Verify the username matches what we stored in session
                    var sessionUsername = HttpContext.Session.GetString("PreAuthUsername");
                    if (!string.IsNullOrEmpty(sessionUsername) && account.user_name != sessionUsername)
                    {
                        _logger.LogWarning($"Username mismatch for account ID: {account.id}. Session: {sessionUsername}, Account: {account.user_name}");
                        ViewBag.Error = "Session validation failed. Please login again.";
                        ClearOtpSession();
                        postData.otp_sent = false;
                        return View("Index", postData);
                    }

                    // Complete login process (cookies, identity, password expiry, redirect)
                    return await CompleteLoginAsync(account, postData);
                }
                catch (Exception err)
                {
                    ViewBag.Error = err.Message;
                    ClearOtpSession();
                    postData.otp_sent = false;
                    return View("Index", postData);
                }
            }
        }


        // OLD CheckOtp method removed - OTP functionality is now handled in the Login method
        // This method was causing conflicts with the new OTP flow

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            Response.Cookies.Delete("Account");
            Response.Cookies.Delete("Role");
            Response.Cookies.Delete("LoggedIn");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Eror()
        {
            return View("Eror");
        }

        public string GetClientIp()
        {
            var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;

            if (remoteIpAddress != null && remoteIpAddress.IsIPv4MappedToIPv6)
            {
                remoteIpAddress = remoteIpAddress.MapToIPv4();
            }

            return remoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        /// <summary>
        /// Completes the login process by setting cookies, creating identity, handling password expiry, and redirecting based on role.
        /// </summary>
        private async Task<IActionResult> CompleteLoginAsync(AccountVM account, LoginVM postData)
        {
            try
            {
                var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? GetClientIp();
                await loginRepository.LogEntry(account.id, account.user_name ?? "", remoteIpAddress ?? "");

                // Set Cookies
                Response.Cookies.Append("Account", _protector.Protect(account.id.ToString()));
                Response.Cookies.Append("Role", _protector.Protect(account.role_name ?? ""));
                Response.Cookies.Append("LoggedIn", _protector.Protect(account.staff_name?.ToString() ?? ""));
                Response.Cookies.Append("SSA", _protector.Protect(account.ssa_code ?? ""));
                Response.Cookies.Append("Circle", _protector.Protect(account.circle ?? ""));
                Response.Cookies.Append("SessionUser", _protector.Protect(account.user_name?.ToString() ?? ""));  //corrected on 26-12-2025
                Response.Cookies.Append("Pwdflag", _protector.Protect(account.changepassword?.ToString() ?? ""));

                // Create the identity for the user
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, account.role_name ?? ""),
                    new Claim(ClaimTypes.PrimarySid, _protector.Protect(account.id.ToString()))
                }, CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Clear OTP session (if any)
                ClearOtpSession();

                // Handle password expiry
                var pwdflg = account.changepassword;
                if (String.Equals(pwdflg, "1"))
                {
                    TempData["PasswordMessage"] = "Password expired .Please change your password ";
                    return RedirectToAction("Pwd_Change", "Users");
                }

                // Check password expiry (30 days)
                if (!string.IsNullOrEmpty(account.reset_on))
                {
                    DateTime futurDate = Convert.ToDateTime(account.reset_on, CultureInfo.GetCultureInfo("hi-IN").DateTimeFormat);
                    DateTime TodayDate = DateTime.Now;
                    int numberOfDays = ((int)(TodayDate - futurDate).TotalDays);
                    if (numberOfDays > 30)
                    {
                        try
                        {
                            int nid = int.Parse(account.id.ToString());
                            var result = await loginRepository.PwdExpiryupdt(nid);
                            if (result == "success")
                            {
                                TempData["PasswordMessage"] = "Password expired (more than 30 days) .Please change your password ";
                                return RedirectToAction("Pwd_Change", "Users");
                            }
                            else
                            {
                                ViewBag.Error = "Expiry password error";
                                return View("Index", postData);
                            }
                        }
                        catch (Exception err)
                        {
                            ViewBag.Error = err.Message;
                            return View("Index", postData);
                        }
                    }
                }

                // Redirect based on role
                return RedirectToRoleBasedPage(account.role_name);
            }
            catch (Exception err)
            {
                _logger.LogError(err, "Error completing login for account ID: {AccountId}", account.id);
                ViewBag.Error = err.Message;
                ClearOtpSession();
                postData.otp_sent = false;
                return View("Index", postData);
            }
        }

        /// <summary>
        /// Redirects user to the appropriate page based on their role.
        /// </summary>
        private IActionResult RedirectToRoleBasedPage(string? roleName)
        {
            switch (roleName)
            {
                case "super_admin":
                    return RedirectToAction("Index", "BsnlDash");
                case "circle_admin":
                    return RedirectToAction("Index", "UserDash");
                case "circle_view":
                    return RedirectToAction("Index", "BsnlDash");
                case "ba_admin":
                    return RedirectToAction("Index", "SsaAdminDash");
                case "cc_admin":
                    return RedirectToAction("Index", "UserDash");
                case "csc_admin":
                    return RedirectToAction("Index", "CscAdminDash");
                case "reports_view":
                    return RedirectToAction("Index", "Summary");
                default:
                    return RedirectToAction("Index", "UserDash");
            }
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("PreAuthAccountId");
            HttpContext.Session.Remove("PreAuthUsername");
            HttpContext.Session.Remove("PreAuthMobile");
            HttpContext.Session.Remove("OTPTime");
            HttpContext.Session.Remove("OTPTryCount");
        }

        private string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString(); // 6-digit OTP
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace cos.Controllers
{
    [Authorize(Roles = "ba_admin")]
    public class SsaAdminDashController : Controller
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<SsaAdminDashController> _logger; //for logging

        public SsaAdminDashController(IConfiguration configuration, IDataProtectionProvider provider, ILogger<SsaAdminDashController> logger)
        {
            _protector = provider.CreateProtector("DataProtector");
            _logger = logger;
        }
        public IActionResult Index()
        {

            var roleCookie = HttpContext.Request.Cookies["Role"];
            var role = string.IsNullOrEmpty(roleCookie) ? string.Empty : _protector.Unprotect(roleCookie);
            if (!string.Equals(role, "ba_admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Access denied. BA admin role required.";
                return RedirectToAction("AccessDenied", "Home");
            }

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

            return View();
        }
        public IActionResult Error()
        {
            return View();
        }



    }
}

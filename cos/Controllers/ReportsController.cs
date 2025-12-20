using cos.Repositories;
using cos.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;


namespace cos.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ReportsRepository reportsRepository;        
        private readonly IDataProtector _protector;

        public ReportsController(IConfiguration configuration, IDataProtectionProvider provider)
        {
            reportsRepository = new ReportsRepository(configuration);
            //  sharedRepository = new SharedRepository(configuration);
            _protector = provider.CreateProtector("DataProtector");
        }
        public IActionResult postpaidsummary()
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


        [HttpPost]
        // [Authorize(Roles = "circle_admin,circle_view,ba_admin")]
        public async Task<JsonResult> GetPostpaidSummary()
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<PostpaidSummaryVM> postSummary = await reportsRepository.GetpostSummary(ViewBag.Circle, role, ssa);
            return Json(postSummary);
        }

        [HttpPost]
        public async Task<JsonResult> GetPostDetails(int circle, string location)
        {
            // You still can use cookies if required
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);

            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<PostpaiddetailsVM> postdetVMs =
                await reportsRepository.Getpostdetails(circle, location, role, ssa);

            return Json(postdetVMs);
        }



    }
}

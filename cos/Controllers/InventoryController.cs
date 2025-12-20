using cos.Repositories;
using cos.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace cos.Controllers
{
    public class InventoryController : Controller
    {
        private readonly InventoryRepository inventoryRepository;
        private readonly IDataProtector _protector;

        public InventoryController(IConfiguration configuration, IDataProtectionProvider provider)
        {
            inventoryRepository = new InventoryRepository(configuration);
            //  sharedRepository = new SharedRepository(configuration);
            _protector = provider.CreateProtector("DataProtector");
        }
        public IActionResult PrepaidDetails()
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            if (!string.IsNullOrEmpty(_cookieRole))
            {
                ViewBag.UserRole = _protector.Unprotect(_cookieRole);
            }
            return View();
        }
        public IActionResult PushInventory()
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            return View();
        }
        //public IActionResult UploadGSMInventory()
        //{
        //    var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
        //    ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
        //    return View();
        //}
        //public IActionResult RemoveCTOPAadhaar()
        //{
        //    var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
        //    ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
        //    return View();
        //}
        public async Task<JsonResult> GetCircles()
        {
            var result = await inventoryRepository.GetCircles();
            return Json(result);
        }
        public async Task<JsonResult> UploadSpareGSMNumbers(string filedata)
        {
            var _cookieSessionUser = HttpContext.Request.Cookies["SessionUser"];
            var username = _protector.Unprotect(_cookieSessionUser).ToString();
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            var circle = _protector.Unprotect(_cookiecircle).ToString();
            var result = await inventoryRepository.UploadSpareGSMNumbersFast(filedata, circle, username);
            return Json(result);
        }
        public async Task<JsonResult> GetCtopDetails(string ctopupno)
        {
            var result = await inventoryRepository.GetCtopDetails(ctopupno);
            return Json(result);
        }
        public async Task<string> SetAadhaarToNull(string ctopupno)
        {
            var result = await inventoryRepository.SetAadhaarToNull(ctopupno);
            return result;
        }
        [HttpPost]
        public async Task<JsonResult> GetPrepaidSummary()
        {
            var result = await inventoryRepository.GetPrepaidSummary();
            return Json(result);
        }
        public async Task<JsonResult> GetPrepaidDetails(int circle_code, string location, int status)
        {
            var result = await inventoryRepository.GetPrepaidDetails(circle_code, location, status);
            return Json(result);
        }
    }
}

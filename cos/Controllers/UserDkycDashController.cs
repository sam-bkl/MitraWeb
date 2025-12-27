using cos.Repositories;
using cos.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using YourProject.Repositories.Interfaces;

namespace cos.Controllers
{
    public class UserDkycDashController : Controller
    {
        private readonly UserDkycDashRepository userDkycDashRepository;
        //  private readonly SharedRepository sharedRepository;
        private readonly IDataProtector _protector;
        private readonly IOracleRepository oracleRepository; //added for oracle handling
        private readonly ILogger<UserDkycDashController> _logger; //for logging

        public UserDkycDashController(IConfiguration configuration, IDataProtectionProvider provider, ILogger<UserDkycDashController> logger)
        {
            userDkycDashRepository = new UserDkycDashRepository(configuration);
            oracleRepository = new OracleRepository(configuration);
            _protector = provider.CreateProtector("DataProtector");
            _logger = logger;
        }
        public IActionResult Index()
        {
            // Check if user is authenticated first
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
                ViewBag.LoggedIn = !string.IsNullOrEmpty(_cookieLoggedIn) ? _protector.Unprotect(_cookieLoggedIn) : "";
                
                var _cookiecircle = HttpContext.Request.Cookies["Circle"];
                ViewBag.Circle = !string.IsNullOrEmpty(_cookiecircle) ? _protector.Unprotect(_cookiecircle) : "";
                
                var _cookieRole = HttpContext.Request.Cookies["Role"];
                if (!string.IsNullOrEmpty(_cookieRole))
                {
                    ViewBag.UserRole = _protector.Unprotect(_cookieRole);
                }

                //code to check password expiry
                var _Pwdflag = HttpContext.Request.Cookies["Pwdflag"];
                if (!string.IsNullOrEmpty(_Pwdflag))
                {
                    var pwdflg = _protector.Unprotect(_Pwdflag);
                    if (String.Equals(pwdflg, "1"))
                    {
                        TempData["PasswordMessage"] = "Password expired .Please change your password ";
                        return RedirectToAction("Pwd_Change", "Users");
                    }
                }
                //code to check password expiry
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting cookies in UserDash/Index");
                // If cookies are invalid, redirect to login
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
        public IActionResult Error()
        {
            return View();
        }




    public IActionResult dkycpendingrequests()
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
        public async Task<JsonResult> GetDkycDetails(string cafType)
        {
            var _cookiecircle = HttpContext.Request.Cookies["Circle"];
            ViewBag.Circle = _protector.Unprotect(_cookiecircle);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            var role = _protector.Unprotect(_cookieRole);
            var _cookieSsa = HttpContext.Request.Cookies["SSA"];
            var ssa = _protector.Unprotect(_cookieSsa);

            List<kycVM> kycVMs = await userDkycDashRepository.GetDkycdetails(ViewBag.Circle, role, ssa, cafType);
            return Json(kycVMs);
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
            InventoryVM inventories = await userDkycDashRepository.GetInventory(Customer, ViewBag.Circle,role,ssa);

            return Json(inventories);
        }


[HttpGet]
        public async Task<IActionResult> CAFFormDkyc(string cafslno)
        {
            var _cookieLoggedIn = HttpContext.Request.Cookies["LoggedIn"];
            ViewBag.LoggedIn = _protector.Unprotect(_cookieLoggedIn);
            var _cookieRole = HttpContext.Request.Cookies["Role"];
            if (!string.IsNullOrEmpty(_cookieRole))
            {
                ViewBag.UserRole = _protector.Unprotect(_cookieRole);
            }

            if (string.IsNullOrEmpty(cafslno))
                return BadRequest("cafslno is required");

            var model = await userDkycDashRepository.GetCAFDataAsync(cafslno);

            if (model == null)
                return NotFound($"CAF data not found for slno: {cafslno}");

            // FILL LOCAL ADDRESS HERE
           // model.FillLocalAddressIfEmpty();

            // if (model.Photo != null && model.Photo.Length > 0)
            // {
            //     model.PhotoBase64 = Convert.ToBase64String(model.Photo);
            // }

            // //get photos
            // if (model.LivePhotoTime != null)
            // {
            //     var date = model.LivePhotoTime.Value.ToString("yyyy-MM-dd");
            //     model.PhotoUrl = Url.Action("GetCAFPhoto", "UserDash", new { cafno = model.Caf_Serial_No, date });
            // }




            return View(model);
        }








        
    }
}


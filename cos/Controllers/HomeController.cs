using cos.Models;
using cos.Repositories;
using cos.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using DNTCaptcha.Core;
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

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IDataProtectionProvider provider)
        {
            _logger = logger;
            loginRepository = new LoginRepository(configuration);
            _protector = provider.CreateProtector("DataProtector");
        }

        public IActionResult Index()
        {
            ViewBag.Error = "";
            return View();
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


            ViewBag.Error = "";
            AccountVM account = new AccountVM();

            if (ModelState.IsValid)
            {
                try
                {
                    account = await loginRepository.Authenticate(postData);

                    if (account != null)

                    {
                        if (account.id > 0)
                        {
                            if (account.is_verified == "VERIFIED")
                            {
                                var remoteIpAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                                // var remoteIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();


                                await loginRepository.LogEntry(account.id, account.user_name, remoteIpAddress);

                                var userRole = account.role_name;

                                // Set Cookies
                                Response.Cookies.Append("Account", _protector.Protect(account.id.ToString()));
                                Response.Cookies.Append("Role", _protector.Protect(account.role_name));
                                Response.Cookies.Append("LoggedIn", _protector.Protect(account.staff_name.ToString()));
                                Response.Cookies.Append("SSA", _protector.Protect(account.ssa_code));
                                Response.Cookies.Append("Circle", _protector.Protect(account.circle));
                                Response.Cookies.Append("SessionUser", _protector.Protect(account.user_name.ToString()));
                                Response.Cookies.Append("Pwdflag", _protector.Protect(account.changepassword.ToString()));

                                //Create the identity for the user  
                                var identity = new ClaimsIdentity(new[]
                                {
                                new Claim(ClaimTypes.Role, account.role_name)
                            }, CookieAuthenticationDefaults.AuthenticationScheme);

                                var principal = new ClaimsPrincipal(identity);
                                var login = HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


                                var pwdflg = account.changepassword;
                                if (String.Equals(pwdflg, "1"))
                                {
                                    TempData["PasswordMessage"] = "Password expired .Please change your password ";
                                    return RedirectToAction("Pwd_Change", "Users");
                                }

                                //  DateTime futurDate = Convert.ToDateTime(account.reset_on);
                                //   DateTime futurDate = DateTime.ParseExact("dd/MM/yyyy", futurDate, CultureInfo.InvariantCulture);
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
                                            return View("Index");
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        ViewBag.Error = err.Message;
                                    }

                                }

                                //  if (RedirectUrl == null)
                                // {
                                switch (account.role_name)
                                {
                                    case "super_admin": // super_admin
                                        return RedirectToAction("Index", "BsnlDash");
                                    //return RedirectToAction("Index", new { ssaId = postData.ssa_id });
                                    case "circle_admin": // super_admin
                                        return RedirectToAction("Index", "UserDash");
                                    //return RedirectToAction("Index", new { ssaId = postData.ssa_id });
                                    case "circle_view": // super_admin
                                        return RedirectToAction("Index", "BsnlDash");
                                    case "ba_admin": // ba_admin
                                        return RedirectToAction("Index", "SsaAdminDash");
                                    //return RedirectToAction("Index", new { ssaId = postData.ssa_id });

                                    case "cc_admin": // cc agent
                                        return RedirectToAction("Index", "UserDash");

                                    case "csc_admin": // csc_admin
                                        return RedirectToAction("Index", "CscAdminDash");
                                    case "reports_view": // REPORTS
                                        return RedirectToAction("Index", "Summary");

                                }
                                // }
                                // else
                                // {
                                //    return Redirect(RedirectUrl);
                                // }


                            }
                            else
                            {
                                ViewBag.Error = "Login failed! Your account has not yet been verified. Please contact your system administrator.";
                                return View("Index");
                            }
                        }
                        else
                        {

                            TempData["shortMessage"] = "Login failed! These credentials do not match our record";
                            return RedirectToAction("Login", "Home");
                        }
                    }
                    else
                    {

                        ViewBag.Error = "Login failed! These credentials do not match our record";
                        return View("Index");

                    }


                }
                catch (Exception err)
                {
                    ViewBag.Error = err.Message;
                }
            }
            return View("Index");
        }


        [HttpPost]

        public async Task<JsonResult> CheckOtp(LgchkVM postdata)
        {

            ViewBag.Error = "";

            LgchkVM lgchk = new LgchkVM();

            if (ModelState.IsValid)
            {

                //  TempData["alertMessage"] = postdata.account_no;
                //  return View("ShowBillaccounts");

                try
                {

                    lgchk = await loginRepository.ChkData(postdata);
                    if (lgchk != null)
                    {


                        // TempData["alertMessage"] = "ok";
                        Random random = new Random();
                        string otp = (random.Next(100100, 999990)).ToString();

                        // var remoteIpAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                        string remoteIpAddress = GetClientIp();
                        await loginRepository.OtpLogEntry(lgchk, remoteIpAddress, otp);

                        string result = GetOtp1(lgchk.mobile.ToString(), otp);
                        JObject jsonObject = JObject.Parse(result);
                        string messageId = jsonObject["Message_Id"].Value<string>();
                        string myerror = jsonObject["Error"].Value<string>();

                        if (messageId?.Length > 0)
                        {
                            TempData["alertMessage"] = "OTP sent to your registered Mobile Number";
                            return Json(TempData);
                            //    //return View("ShowBillaccounts");
                        }
                        else
                        {
                            TempData["alertMessage"] = myerror;
                            //   TempData["alertMessage"] = "OTP facility is currently out of service.pl try after sometime";
                            return Json(TempData);
                            //return View("ShowBillaccounts");
                        }


                    }
                    else
                    {
                        TempData["alertMessage"] = "Sorry!These credentials do not match our record";
                        return Json(TempData);
                        //return View("ShowBillaccounts");
                        // errors.Add(result);

                    }
                }
                catch (Exception err)
                {
                    //errors.Add(err.Message);
                    TempData["alertMessage"] = err.Message;
                    return Json(TempData);
                }
            }


            //  ViewBag.Errors = errors;
            // return View("Create");
            // ViewBag.Error = "Please check your credentials";
            else
            {
                TempData["alertMessage"] = "Please enter your credentials";
                return Json(TempData);
            }
            //if everything fails
            TempData["alertMessage"] = "System error.pl try after sometime";
            return Json(TempData);
        }
        public string GetOtp1(string mobileno, string otp) //using proxy
        {
            HttpClientHandler clientHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                // Proxy = new WebProxy("http://10.44.1.234:3128") //Testing from KRL CO
                // Proxy = new WebProxy("http://10.199.222.230:8080")
            };

            using (var client = new HttpClient(clientHandler))
            {

                client.BaseAddress = new Uri("https://bulksms.bsnl.in:5010/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IjEwMDIwIDQiLCJuYmYiOjE3Mzk1MDExMjAsImV4cCI6MTc3MTAzNzEyMCwiaWF0IjoxNzM5NTAxMTIwLCJpc3MiOiJodHRwczovL2J1bGtzbXMuYnNubC5pbjo1MDEwIiwiYXVkIjoiMTAwMjAgNCJ9.wN4uI76PTADLrSyGMKno0BrlmzgpU4SxhZ20MjDNiGE");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                //Random random = new Random();
                // int randomNumber = random.Next(100100, 999990);

                var keyData = new List<object>();
                keyData.Add(new { Key = "otp", Value = otp });
                // keyData.Add(new { Key = "site", Value = "pgrms.bsnl.co.in" });
                var inputData = new
                {
                    Header = "BSNLPG",
                    Target = mobileno,
                    Is_Unicode = "0",
                    Is_Flash = "0",
                    Message_Type = "SI",
                    Entity_Id = "1401601530000015602",
                    Content_Template_Id = "1407174564682191263",
                    Template_Keys_and_Values = keyData
                };

                var smspostData = JsonConvert.SerializeObject(inputData);

                StringContent content = new StringContent(smspostData, Encoding.UTF8, "application/json");

                var response = client.PostAsync("api/Send_SMS", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    return "Error";
                }
            }


        }

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

            return remoteIpAddress?.ToString();
        }
    }
}
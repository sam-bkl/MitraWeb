using Microsoft.AspNetCore.Mvc;
using cos.Repositories;
using cos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace cos.Controllers
{
    [Authorize(Roles = "reports_view,circle_admin")]
    public class SummaryController : Controller
    {
        private readonly SummaryRepository _summaryRepository;
        private readonly IUserCookieContextAccessor _userContext;
  

        public SummaryController(IConfiguration configuration,IUserCookieContextAccessor userContext,SummaryRepository summaryRepository)
        {
            _summaryRepository = summaryRepository;
            _userContext = userContext;            

        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DayWiseActivation()
        {
            return View();
        }

        public ActionResult DayWiseOnboarding()
        {
            return View();
        }

        public ActionResult CircleWiseBillingStatus()
        {
            return View();
        }


        public ActionResult BillingStatus()
        {
            return View();
        }

        public ActionResult GSMInventory()
        {
            return View();
        }

        [Authorize(Roles = "ba_admin,circle_admin")]
        public ActionResult CafStatus()
        {
            return View();
        }


        /// <summary>
        /// KYC details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetKycDetails()
        {

            var data = await _summaryRepository.GetKycSummaryCircleWiseAsyncCached();

            return Json(new
            {
                success = true,
                data
            });
        }  


        /// <summary>
        /// activation details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetActivationDetails()
        {

            var data = await _summaryRepository.GetActivatedCircleWiseAsync();

            return Json(new
            {
                success = true,
                data
            });
        }   
    
            /// <summary>
        /// activation details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetActivationDaily(DateTime startDate, DateTime endDate)
        {


            var data = await _summaryRepository.GetDayWiseActivationAsync(startDate, endDate);

            return Json(new
            {
                success = true,
                data
            });
        }   
    
                /// <summary>
        /// activation details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetKycOnboardingDaily(DateTime startDate, DateTime endDate)
        {


            var data = await _summaryRepository.GetDayWiseOnboardingAsync(startDate, endDate);

            return Json(new
            {
                success = true,
                data
            });
        }  

        /// <summary>
        /// activation details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetCircleWiseBillingStatus()
        {


            var data = await _summaryRepository.GetCircleWiseBillingStatusAsync();

            return Json(new
            {
                success = true,
                data
            });
        } 

        /// <summary>
        /// activation details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetBillingStatus()
        {


            var data = await _summaryRepository.GetBillingStatusAsync();

            return Json(new
            {
                success = true,
                data
            });
        } 
        /// <summary>
        /// KYC details grouped by Circle, POS and TYPE
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetGSMInventoryCount()
        {

            var data = await _summaryRepository.CircleWiseGSMInvetoryAsync();

            return Json(new
            {
                success = true,
                data
            });
        }  


        /// <summary>
        /// Fetch CAF summary using GSM number or CAF serial number
        /// </summary>
        
        [HttpGet]
        [Authorize(Roles = "ba_admin,circle_admin")]
        public async Task<JsonResult> GetCafSummary(
            string gsmNumber = null,
            string cafSerialNo = null)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(gsmNumber) &&
                string.IsNullOrWhiteSpace(cafSerialNo))
            {
                return Json(new
                {
                    success = false,
                    message = "Either GSM number or CAF serial number is required."
                });
            }

            var user = _userContext.Get();
            // int? circle = user.Circle == null ? null : int.Parse(user.Circle);

            Console.WriteLine(user.Circle);
            try
            {
                var cafDetails = await _summaryRepository.GetCafSummaryAsync(
                    cafSerialNo,
                    gsmNumber
                    
                );
                // Console.WriteLine((string)cafDetails?.caf_serial_no);

                if (cafDetails == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No record found."
                    });
                }
                object activationDetails = null;
                // if cos_bcd haiving data and approved
                // the activation status can be pulled
                if (cafDetails?.verified_flag == "Y")
                {
                    activationDetails = await _summaryRepository.GetCafActivationByCafAsync(
                    cafSerialNo = cafDetails.caf_serial_no
                    );

                }

                return Json(new
                {
                    success = true,
                    data = new{
                        cafDetails,
                        activationDetails
                    }
                });
            }
            catch (Exception)
            {
                // Generic safety net
                throw;
                // return Json(new
                // {
                //     success = false,
                //     message = "An error occurred while fetching CAF details."
                // });
            }
        }

    }


}

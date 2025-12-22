using Microsoft.AspNetCore.Mvc;
using cos.Repositories;

using Microsoft.AspNetCore.Authorization;

namespace cos.Controllers
{
    [Authorize(Roles = "reports_view,circle_admin")]
    public class SummaryController : Controller
    {
        private readonly SummaryRepository _summaryRepository;
  

        public SummaryController(IConfiguration configuration,SummaryRepository summaryRepository)
        {
            _summaryRepository = summaryRepository;

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

    
    }
}

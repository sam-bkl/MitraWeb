using Microsoft.AspNetCore.Mvc;
using cos.Repositories;
using Microsoft.AspNetCore.Authorization;
using cos.Interfaces;

namespace cos.Controllers
{
    [Authorize(Roles = "circle_admin,ba_admin")]
    public class PosController : Controller
    {
        private readonly PosRepository _posRepository;
        private readonly IUserCookieContextAccessor _userContext;
  

        public PosController(IConfiguration configuration,IUserCookieContextAccessor userContext,PosRepository posRepository)
        {
            _posRepository = posRepository;
            _userContext = userContext;
        }

        public ActionResult Index()
        {
            return View();
        }



        /// <summary>
        /// GetCtopupCircleMasterAsync
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetCtopupMaster()
        {
            var user = _userContext.Get();
    
            object data ;
            switch (user.Role)
            {
                case "circle_admin" when user.Circle != null:
                    data = await _posRepository.CtopupCircleMasterAsync(user.Circle);
                    return Json(new { success = true, data });
                case "ba_admin" when user.SSA != null && user.Circle !=null:
                    data = await _posRepository.CtopupBaMasterAsync(user.Circle,user.SSA);
                    return Json(new { success = true, data });                    

                default:
                    return Json(new { success = false, data = "" });
            }
  

        }  

        /// <summary>
        /// GetCtopupCircleMasterAsync
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetCtopupSummaryMaster()
        {

            var user = _userContext.Get();

            object data ;
            switch (user.Role)
            {
                case "circle_admin" when user.Circle != null:
                    data = await _posRepository.CtopupCircleSummaryAsync(user.Circle);
                    return Json(new { success = true, data });
                case "ba_admin" when user.SSA != null && user.Circle !=null:
                    data = await _posRepository.CtopupBaSummaryAsync(user.Circle, user.SSA);
                    return Json(new { success = true, data });                    

                default:
                    return Json(new { success = false, data = "" });
            }
  

        }  
        



    
    }
}

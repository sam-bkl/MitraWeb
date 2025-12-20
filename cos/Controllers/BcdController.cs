using Microsoft.AspNetCore.Mvc;
using YourProject.Repositories.Interfaces;   // 🔥 important
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

public class BcdController : Controller
{
    private readonly IOracleRepository _oracleRepo;

    public BcdController(IOracleRepository oracleRepo)
    {
        _oracleRepo = oracleRepo;
    }

    [HttpPost]
    public async Task<JsonResult> GetBcdDetails()
    {
        var data = await _oracleRepo.GetBcdDetails();
        return Json(data);
    }

    // Optional View loader if you need a UI
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> TestConnection()
    {
        var result = await _oracleRepo.TestOracleConnection();
        return Json(new { message = result });
    }

    [HttpGet]
    public IActionResult TestConnectionView()
    {
        return View("TestConnection");
    }

}

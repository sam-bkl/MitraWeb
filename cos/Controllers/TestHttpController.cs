using Microsoft.AspNetCore.Mvc;

namespace cos.Controllers
{
    [Route("test-http")]
    public class TestHttpController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("photo")]
        public async Task<IActionResult> TestPhoto()
        {
            string testUrl = "http://10.201.222.68:9801/cos_images/2025/12/08/BEC0002330p.jpg";

            try
            {
                var handler = new HttpClientHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using (var http = new HttpClient(handler))
                {
                    http.Timeout = TimeSpan.FromSeconds(20);

                    HttpResponseMessage resp = await http.GetAsync(testUrl, HttpCompletionOption.ResponseHeadersRead);

                    if (!resp.IsSuccessStatusCode)
                    {
                        return Content("HTTP FAIL: " + resp.StatusCode);
                    }

                    var stream = await resp.Content.ReadAsStreamAsync();
                    return File(stream, "image/jpeg");
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR: " + ex.Message);
            }
        }


        [HttpGet("ping")]
        public async Task<IActionResult> TestPing()
        {
            string testUrl = "http://10.201.222.68:9801";

            try
            {
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(100);

                    var response = await http.GetAsync(testUrl);
                    return Content($"Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return Content("ERROR: " + ex.Message);
            }
        }
    }
}

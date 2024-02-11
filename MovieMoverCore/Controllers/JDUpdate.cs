using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using MovieMoverCore.Services;
using System.Threading.Tasks;

namespace MovieMoverCore.Controllers
{
    [ApiController]
    [Route("/api/{controller}/{action=Index}")]
    public class JDUpdateController : Controller
    {

        private IJDownloader _jDownloader;

        public JDUpdateController(IJDownloader jDownloader)
        {
            _jDownloader = jDownloader;
        }

        [HttpGet]
        public string Index()
        {
            return "Hello World - 42";
        }

        [HttpGet]
        public string JDState()
        {
            return _jDownloader.GetCurrentState().ToString();
        }

        [HttpGet]
        public async Task<IActionResult> IsUpdateAvailable()
        {
            var result = await _jDownloader.IsNewUpdateAvailable();
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> InvokeUpdate()
        {
            if (ModelState.IsValid)
            {
                var result = await _jDownloader.UpdateAndRestart();
                if (result)
                {
                    return new OkResult();
                }
                else
                {
                    return StatusCode(500);
                }
            }
            return new BadRequestResult();
        }

        [HttpPost]
        public async Task<IActionResult> InvokeUpdateCheck()
        {
            if (ModelState.IsValid)
            {
                var result = await _jDownloader.CheckForUpdate();
                if (result)
                {
                    return new OkResult();
                }
                else
                {
                    return StatusCode(500);
                }
            }
            return new BadRequestResult();
        }
    }
}

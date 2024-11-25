using Microsoft.AspNetCore.Mvc;
using MovieMoverCore.Models;
using MovieMoverCore.Services;
using System.Collections.Generic;

namespace MovieMoverCore.Controllers
{
    [ApiController]
    [Route("/api/{controller}/{action}")]
    public class MaintenanceController : Controller
    {
        private readonly ISettings _settings;

        public MaintenanceController(ISettings settings)
        {
            _settings = settings;
        }

        [HttpPost]
        public IActionResult ReloadRenamings()
        {
            _settings.LoadRenamings();
            return new OkResult();
        }
    }
}

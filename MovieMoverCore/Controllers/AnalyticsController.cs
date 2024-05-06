using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Helpers;
using MovieMoverCore.Services;
using System.Collections.Generic;
using System;
using System.Linq;

namespace MovieMoverCore.Controllers
{
    [ApiController]
    [Route("/api/{controller}/{action}")]

    public class AnalyticsController : Controller
    {
        private readonly ISharedData<(DateTime requestTime, TimeSpan duration, string request)> _sharedTimings;
        public AnalyticsController(ISharedData<(DateTime requestTime, TimeSpan duration, string request)> sharedTimings)
        {
            _sharedTimings = sharedTimings;
        }

        [HttpGet]
        public IActionResult Timings()
        {
            return new JsonResult(_sharedTimings["REQUEST_TIMINGS"].Select(t => new
            {
                t.requestTime,
                t.duration,
                t.request
            }));
        }
    }
}

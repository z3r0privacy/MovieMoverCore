using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using MovieMoverCore.Services;
using System.Linq;
using System.Text.Json;

namespace MovieMoverCore.Controllers
{
    [ApiController]
    [Route("/api/{controller}/{action}")]
    public class ObtainController : Controller 
    {
        private readonly IFileBasedDatabase<Obtain> _obtains;
        private readonly ILogger<ObtainController> _logger;

        public ObtainController(IFileBasedDatabase<Obtain> obtains, ILogger<ObtainController> logger)
        {
            _obtains = obtains;
            _logger = logger;
        }

        [Route("~/api/{controller}")]
        [HttpGet]
        public IActionResult List()
        {
            _logger.LogDebug("Getting all obtains");
            var objs = _obtains.ToList();
            _logger.LogDebug($"Returning {objs.Count} objects");
            return new JsonResult(objs);
        }

        [Route("~/api/{controller}/{idx}")]
        [HttpGet]
        public IActionResult Get(int idx)
        {
            _logger.LogDebug($"Requesting Obtain with id '{idx}'");
            var o = _obtains[idx];
            if (o == null)
            {
                _logger.LogWarning($"Could not find Obtain with id '{idx}'");
                return new NotFoundResult();
            }
            var s = o.ReleaseDate?.ToString("dd.MM.yyyy");
            _logger.LogDebug($"Returning Obtain with id '{idx}': '{o.Name}'");
            return new JsonResult(new
            {
                o.Id,
                o.Name,
                ReleaseDate = s
            });
        }

        [HttpPost]
        public IActionResult AddObtain(Obtain obtain)
        {
            if (ModelState.IsValid)
            {
                _logger.LogDebug($"Adding new Obtain '{obtain.Name}'");
                _obtains.Add(obtain);
                _logger.LogInformation($"Added new Obtain ({obtain.Id}) '{obtain.Name}'");
                return new OkResult();
            } else
            {
                _logger.LogInformation($"Obtain was not added due to invalid Modelstate");
                return new BadRequestResult();
            }
        }

        [HttpPut]
        public IActionResult UpdateObtain(Obtain obtain)
        {
            if (ModelState.IsValid)
            {
                _logger.LogDebug($"Updating Obtain '{obtain.Id}'");
                _obtains.Update(obtain);
                _logger.LogInformation($"Updated Obtain ({obtain.Id}) '{obtain.Name}'");
                return new OkResult();
            } else
            {
                _logger.LogInformation($"Obtain was not updated due to invalid Modelstate");
                return new BadRequestResult();
            }
        }

        [Route("~/api/{controller}/{action}/{idx}")]
        [HttpPut]
        public IActionResult Obtained(int idx)
        {
            _logger.LogDebug($"Marking Obtain with id {idx} as obtained");
            var o = _obtains[idx];
            if (o == null)
            {
                _logger.LogInformation($"No Obtain with id {idx} found");
                return new NotFoundResult();
            }
            o.State = ObtainState.Obtained;
            _obtains.Update(o);
            _logger.LogInformation($"Marked Obtain '{o.Name}' ({o.Id}) as obtained");
            return new OkResult();
        }

        [Route("~/api/{controller}/{action}/{idx}")]
        [HttpDelete]
        public IActionResult Delete(int idx)
        {
            _logger.LogDebug($"Deleting Obtain with id {idx}");
            if (_obtains.Remove(idx))
            {
                _logger.LogInformation($"Deleted Obtain {idx}");
                return new OkResult();
            }
            _logger.LogInformation($"Could not find Obtain to delete {idx}");
            return new NotFoundResult();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using MovieMoverCore.Models.DTO;
using MovieMoverCore.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MovieMoverCore.Controllers
{
    [ApiController]
    [Route("/api/{controller}/{action}")]
    public class VideoController :Controller
    {
        private ILogger<VideoController> _logger;
        private IDatabase _database;
        private IFileMover _fileMover;
        private IFileOperationsWorker _fileOperationsWorker;

        public VideoController(ILogger<VideoController> logger, IDatabase database, IFileMover fileMover, IFileOperationsWorker fileOperationsWorker)
        {
            _logger = logger;
            _database = database;
            _fileMover = fileMover;
            _fileOperationsWorker = fileOperationsWorker;
        }

        [HttpGet]
        public IActionResult Series()
        {
            var data = _database.GetSeries(s => !s.IsFinished).OrderBy(s => s.Name);
            return new JsonResult(data);
        }

        [HttpGet]
        public IActionResult FileOperationStates()
        {
            var states = _fileOperationsWorker.QueryStates().Select(s => new
            {
                Value = s.ToString(),
                CurrentState = s.CurrentState.ToString(),
                s.ErrorMessage,
                s.ID
            });
            return new JsonResult(states.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> MoveToSeriesAsync([FromBody] MoveToSeries moves)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var series = _database.GetSeries(moves.SeriesId);
            if (series == null)
            {
                return NotFound("The requested series does not exist");
            }

            if (moves.Season <= 0)
            {
                return BadRequest("Season must be greater than 0");
            }

            var states = new List<FileMoveOperation>();
            foreach (var dl in moves.Downloads)
            {
                var s = _fileMover.CreateSeriesMoveOperation(dl, series, moves.Season);
                states.Add(s);
            }

            series.LastSelectedSeason = moves.Season;
            _database.UpdateSeries(series);
            await _database.SaveSeriesChangesAsync();

            return new OkResult();
        }

        [HttpPost]
        public IActionResult MoveToMovies([FromBody] string[] moves)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var states = new List<FileMoveOperation>();
            foreach (var dl in moves)
            {
                var s = _fileMover.CreateMoviesMoveOperation(dl);
                states.Add(s);
            }

            return new OkResult();
        }

        [HttpPost]
        public IActionResult DismissFmo([FromBody] int id)
        {
            if (ModelState.IsValid)
            {
                if (_fileOperationsWorker.DismissState(id))
                {
                    return new OkResult();
                }
                return new NotFoundResult();
            }
            return new BadRequestResult();
        }
    }
}

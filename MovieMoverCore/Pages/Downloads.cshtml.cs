using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MovieMoverCore.Models;
using MovieMoverCore.Models.DTO;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages
{
    public class DownloadsModel : PageModel
    {
        private IFileMover _fileMover;
        private IFileMoveWorker _fileMoveWorker;
        private IDatabase _database;
        private IJDownloader _jDownloader;


        private string _cardTemplate = @"
<div class=""col-md-4 my-2"">
                <div class=""card"" style=""width: 18rem;"" id=""{0}"" onclick=""toggleSelection('{0}');"">
                    <div class=""card-body"">
                        <h6 class=""card-subtitle mb-2 text-muted"">{0}</h6>
                        <i>State: {1}</i>
                    </div>
                </div>
            </div>
";

        public DownloadsModel (IFileMover fileMover, IDatabase database, IFileMoveWorker fileMoveWorker, IJDownloader jDownloader)
        {
            _fileMover = fileMover;
            _database = database;
            _fileMoveWorker = fileMoveWorker;
            _jDownloader = jDownloader;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnGetDownloadsAsync()
        {
            var downloadStatesTask = _jDownloader.QueryDownloadStatesAsync();
            var sb = new StringBuilder();
            var downloads = _fileMover.GetDownloadEntries().OrderBy(f => f);
            if (downloads.Any())
            {
                foreach (var f in downloads)
                {
                    var name = Path.GetFileName(f);

                    var stateData = (await downloadStatesTask).FirstOrDefault(p => Path.GetFileName(p.SaveTo) == name);
                    string state;
                    if (stateData == null)
                    {
                        state = "Unknown";
                    } else
                    {
                        state = stateData.PackageState switch
                        {
                            JD_PackageState.Decrypt => "Decrypting...",
                            JD_PackageState.Download => $"Downloading... {stateData.DownloadPercentage:0}%",
                            JD_PackageState.Extract => "Extracting...",
                            JD_PackageState.Wait => "Waiting to start...",
                            _ => stateData.PackageState.ToString(),
                        };
                    }

                    sb.AppendLine(string.Format(_cardTemplate, name, state));
                }
            } else
            {
                sb.AppendLine("<i>No Downloads...");
            }
            return new JsonResult(JsonSerializer.Serialize(sb.ToString()));
        }

        public IActionResult OnGetSeries()
        {
            var data = _database.GetSeries(s => !s.IsFinished).OrderBy(s => s.Name).Select(s => new
            {
                s.Id,
                s.Name
            });
            return new JsonResult(JsonSerializer.Serialize(data));
        }

        public IActionResult OnPostMoveSeries([FromBody] MoveToSeries moveToSeries)
        {
            var series = _database.GetSeries(moveToSeries.SeriesId);
            if (series == null)
            {
                return BadRequest("The requested series does not exist");
            }

            if (moveToSeries.Season <= 0)
            {
                return BadRequest("Season must be greater than 0");
            }

            var states = new List<FileMoveOperation>();
            foreach (var dl in moveToSeries.Downloads)
            {
                var s = _fileMover.CreateSeriesMoveOperation(dl, series, moveToSeries.Season);
                states.Add(s);
            }

            return new OkResult();
        }

        public IActionResult OnPostMoveMovies([FromBody] string[] moveToMovies)
        {
            var states = new List<FileMoveOperation>();
            foreach (var dl in moveToMovies)
            {
                var s = _fileMover.CreateMoviesMoveOperation(dl);
                states.Add(s);
            }

            return new OkResult();
        }

        public IActionResult OnGetMoveStates()
        {
            var states = _fileMoveWorker.QueryStates().Select(s => new
            {
                s.Name,
                CurrentState = s.CurrentState.ToString(),
                s.ErrorMessage,
                s.ID
            });
            return new JsonResult(JsonSerializer.Serialize(states));
        }

        public IActionResult OnPostDismissFmo([FromBody] int id)
        {
            if (ModelState.IsValid && _fileMoveWorker.DismissState(id))
            {
                return new OkResult();
            }
            return new NotFoundResult();
        }
    }
}
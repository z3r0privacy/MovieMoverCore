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


        private static string _cardTemplateDownloads = @"
<div class=""col-md-4 my-2"">
    <div class=""card"" style=""width: 18rem;"" id=""{0}"" onclick=""toggleSelection('{0}');"">
        <div class=""card-body"">
            <h6 class=""card-subtitle mb-2 text-muted"">{0}</h6>
            <i>State: {1}</i>
        </div>
        <div class=""progress mx-2 mb-2""  style=""height: 4px;"">
            <div class=""progress-bar {3}"" role=""progressbar"" style=""width: {2}%"" aria-valuenow=""25"" aria-valuemin=""0"" aria-valuemax=""100""></div>
        </div>
    </div>
</div>
";

        private static string _cardTemplatePackages = @"
<div class=""col-md-4 my-2"">
    <div class=""card"" style=""width: 18rem;"">
        <div class=""card-body"">
            <h6 class=""card-subtitle mb-2 text-muted"">{0}</h6>
            <div class=""d-flex justify-content-center"">
                <input id=""dlpkg_{1}"" type=""button"" value=""Download"" onclick=""startPackageDownload({1})"" class=""btn btn-primary mx-2"">
                <input id=""rmpkg_{1}"" type=""button"" value=""Remove"" onclick=""removePackage({1})"" class=""btn btn-primary mx-2"">
            </div>
        </div>
    </div>
</div>
";

        public int RefreshRate { get; }

        public DownloadsModel (IFileMover fileMover, IDatabase database, IFileMoveWorker fileMoveWorker, IJDownloader jDownloader, ISettings settings)
        {
            _fileMover = fileMover;
            _database = database;
            _fileMoveWorker = fileMoveWorker;
            _jDownloader = jDownloader;
            RefreshRate = settings.JD_MaxRefreshInterval;
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

                    var stateData = (await downloadStatesTask).FirstOrDefault(p => Extensions.GetFileNamePlatformIndependent(p.SaveTo) == name);
                    string state;
                    string bgprogress;
                    if (stateData == null)
                    {
                        state = "Unknown";
                        bgprogress = "bg-danger";
                    } else
                    {
                        state = stateData.PackageState switch
                        {
                            JD_PackageState.Decrypt => "Decrypting...",
                            JD_PackageState.Download => $"Downloading... {stateData.DownloadPercentage:0}%",
                            JD_PackageState.Extract => stateData.Status, // "Extracting...",
                            JD_PackageState.Wait => "Waiting to start...",
                            _ => stateData.PackageState.ToString(),
                        };
                        bgprogress = stateData.PackageState switch
                        {
                            JD_PackageState.Download => "bg-info",
                            JD_PackageState.Decrypt => "bg-warning",
                            JD_PackageState.Extract => "bg-warning",
                            JD_PackageState.Finished => "bg-success",
                            _ => "bg-danger",
                        };
                    }

                    sb.AppendLine(string.Format(_cardTemplateDownloads, name, state, stateData?.DownloadPercentage ?? 0, bgprogress));
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
                s.Name,
                s.LastSelectedSeason
            });
            return new JsonResult(JsonSerializer.Serialize(data));
        }

        public async Task<IActionResult> OnGetDownloadControllerState()
        {
            var (downloading, speed) = await _jDownloader.QueryDownloadControllerState();
            if (!downloading)
            {
                return new JsonResult(JsonSerializer.Serialize(""));
            }
            var units = new string[] { "B", "KB", "MB", "GB" };
            var unit = 0;
            var speedF = (double)speed;
            while (speedF > 1000 && unit < units.Length)
            {
                speedF /= 1000;
                unit++;
            }
            return new JsonResult(JsonSerializer.Serialize($"Downloading: {speedF:0.00} {units[unit]}/s"));
        }

        public async Task<IActionResult> OnPostMoveSeriesAsync([FromBody] MoveToSeries moveToSeries)
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

            series.LastSelectedSeason = moveToSeries.Season;
            _database.UpdateSeries(series);
            await _database.SaveSeriesChangesAsync();

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

        public async Task<IActionResult> OnGetPendingPackagesAsync()
        {
            var packages = _jDownloader.QueryCrawledPackagesAsync();
            var sb = new StringBuilder();
            var list = new List<object>();
            foreach (var p in await packages)
            {
                var size = $"{(p.BytesTotal / (double)1_048_576_000):f1}"; // 1024*1024=1’048’576 * 1000 = 1’048’576’000 ==> ??? why
                // --> JD propably calculates MBs (1024*1024) and then only moves the dot for displaying GB (therefore 1000)
                sb.Append(string.Format(_cardTemplatePackages, $"{p.Name} ({size} GB)", p.UUID.ToString()));
                list.Add(new
                {
                    Name = $"{p.Name} ({size} GB)",
                    Id = p.UUID.ToString()
                });
            }
            return new JsonResult(JsonSerializer.Serialize(list));
        }

        public async Task<IActionResult> OnPostStartDownloadAsync([FromBody] List<long> uuids)
        {
            if (ModelState.IsValid && await _jDownloader.StartPackageDownloadAsync(uuids))
            {
                return new OkResult();
            }
            return new NotFoundResult();
        }

        public async Task<IActionResult> OnPostAddDownloadLinksAsync([FromBody] string links)
        {
            if (ModelState.IsValid)
            {
                var list = links.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (await _jDownloader.AddDownloadLinksAsync(list))
                {
                    return new OkResult();
                }
                return StatusCode(500);
            }
            return new BadRequestResult();
        }

        public async Task<IActionResult> OnPostRemoveDownloadLinksAsync([FromBody] List<long> uuids)
        {
            if (ModelState.IsValid)
            {
                if (await _jDownloader.RemoveQueriedDownloadLinksAsync(uuids))
                {
                    return new OkResult();
                }
                return StatusCode(500);
            }
            return new BadRequestResult();
        }

        public IActionResult OnPostDismissFmo([FromBody] int id)
        {
            if (ModelState.IsValid && _fileMoveWorker.DismissState(id))
            {
                return new OkResult();
            }
            return new NotFoundResult();
        }

        public async Task<IActionResult> OnPostRestartDownloads()
        {
            var success = await _jDownloader.RestartDownloads();
            return new JsonResult(JsonSerializer.Serialize(success));
        }
    }
}
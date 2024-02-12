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
        private IFileOperationsWorker _fileOperationsWorker;
        private IDatabase _database;
        private IJDownloader _jDownloader;
        private IHistoryCollection<List<string>> _historyCollection;
        private readonly string _urlHistory = "HISTORY_URL_LIST";


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


        public int RefreshRate { get; }

        public DownloadsModel (IFileMover fileMover, IDatabase database, IFileOperationsWorker fileMoveWorker, IJDownloader jDownloader, ISettings settings, IHistoryCollection<List<string>> historyCollection)
        {
            _fileMover = fileMover;
            _database = database;
            _fileOperationsWorker = fileMoveWorker;
            _jDownloader = jDownloader;
            _historyCollection = historyCollection;
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
            return BadRequest("Deprecated. Use GET /api/Video/Series");
        }

        public IActionResult OnGetDownloadControllerState()
        {
            return BadRequest("Deprected. Use ET /api/Downloads/ControllerStatus");
        }

        public IActionResult OnPostMoveSeries([FromBody] MoveToSeries moveToSeries)
        {
            return BadRequest("Deprecated. Use POST /api/Video/MoveToSeries");
        }

        public IActionResult OnPostMoveMovies([FromBody] string[] moveToMovies)
        {
            return BadRequest("Deprecated. Use POST /api/Videos/MoveToMovies");
        }

        public IActionResult OnDeleteDownloads([FromBody] string[] deleteDownloads)
        {
            return BadRequest("Deprecated. Use DELETE /api/Downloads/Remove");
        }

        public IActionResult OnGetFileOperationStates()
        {
            return BadRequest("Deprecated. Use GET /api/Video/FileOperationStates");
        }

        public IActionResult OnGetPendingPackages()
        {
            return BadRequest("Deprecated. Use GET /api/Downloads/PendingPackages");
        }

        public IActionResult OnGetDownloadUrlHistory()
        {
            return BadRequest("Deprecated. Use GET /api/Downloads/UrlHistory");
        }

        public IActionResult OnPostResubmitLinks([FromBody] int histId)
        {
            return BadRequest("Deprecated. Use POST /api/Downloads/ResubmitUrls");
        }

        public IActionResult OnPostStartDownload([FromBody] List<long> uuids)
        {
            return BadRequest("Deprecated. Use POST /api/Downloads/Start");
        }

        public IActionResult OnPostAddDownloadLinks([FromBody] string links)
        {
            return BadRequest("Deprecated. Use POST /api/Downloads/AddUrls");
        }

        public IActionResult OnPostRemoveDownloadLinks([FromBody] List<long> uuids)
        {
            return BadRequest("Deprecated. Use DELETE /api/Downloads/RemovePendingPackages");
        }

        public IActionResult OnPostDismissFmo([FromBody] int id)
        {
            return BadRequest("Deprecated. Use POST /api/Video/DismissFmo");
        }

        public IActionResult OnPostRestartDownloads()
        {
            return BadRequest("Deprecated. Use POST /api/Downloads/Restart");
        }
    }
}
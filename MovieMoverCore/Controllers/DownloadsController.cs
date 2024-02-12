using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Services;
using System;
using System.Threading.Tasks;

namespace MovieMoverCore.Controllers
{
    [ApiController]
    [Route("/api/{controller}/{action=States}")]
    public class DownloadsController : Controller
    {
        private ILogger<DownloadsController> _logger;
        private IJDownloader _jDownloader;
        private IFileMover _fileMover;
        private IFileOperationsWorker _fileOperationsWorker;

        public DownloadsController(ILogger<DownloadsController> logger, IJDownloader jDownloader, IFileMover fileMover, IFileOperationsWorker fileOperationsWorker)
        {
            _logger = logger;
            _jDownloader = jDownloader;
            _fileMover = fileMover;
            _fileOperationsWorker = fileOperationsWorker;
        }

        [HttpGet]
        public async Task<IActionResult> States()
        {
            var downloadStates = await _jDownloader.QueryDownloadStatesAsync();
            return new JsonResult(downloadStates);
        }

        [HttpGet]
        public async Task<IActionResult> CrawledPackages()
        {
            var packages = await _jDownloader.QueryCrawledPackagesAsync();
            return new JsonResult(packages);
        }

        [HttpGet]
        public async Task<IActionResult> ControllerStatus()
        {
            var (downloading, speed) = await _jDownloader.QueryDownloadControllerState();
            if (!downloading)
            {
                return new JsonResult(new
                {
                    Downloading = false,
                    Speed = 0,
                    Unit = "B/s"
                });
            }
            var units = new string[] { "B", "KB", "MB", "GB" };
            var unit = 0;
            var speedF = (double)speed;
            while (speedF > 1000 && unit < units.Length)
            {
                speedF /= 1000;
                unit++;
            }
            return new JsonResult(new
            {
                Unit = $"{units[unit]}/s",
                Speed = Math.Round(speedF, 2),
                Downloading = true
            });
        }

        [HttpDelete]
        public IActionResult Remove([FromBody] string[] downloads)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            bool errOccured = false;
            foreach (var dl in downloads)
            {
                if (_fileMover.IsDownloadNameLegal(dl, out var fullPath))
                {
                    _fileOperationsWorker.QueueDeleteOperation(fullPath);
                }
                else
                {
                    errOccured = true;
                }
            }

            return !errOccured ? new OkResult() : new NotFoundResult();
        }

    }
}

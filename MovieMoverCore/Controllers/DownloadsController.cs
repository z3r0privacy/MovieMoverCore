using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MovieMoverCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private IHistoryCollection<List<string>> _historyCollection;
        private readonly string _urlHistory = "HISTORY_URL_LIST";

        public DownloadsController(ILogger<DownloadsController> logger,
            IJDownloader jDownloader,
            IFileMover fileMover,
            IFileOperationsWorker fileOperationsWorker,
            IHistoryCollection<List<string>> historyCollection)
        {
            _logger = logger;
            _jDownloader = jDownloader;
            _fileMover = fileMover;
            _fileOperationsWorker = fileOperationsWorker;
            _historyCollection = historyCollection;
        }

        [HttpGet]
        public async Task<IActionResult> StatesAsync()
        {
            var downloadStates = await _jDownloader.QueryDownloadStatesAsync();
            return new JsonResult(downloadStates);
        }

        [HttpGet]
        public async Task<IActionResult> CrawledPackagesAsync()
        {
            var packages = await _jDownloader.QueryCrawledPackagesAsync();
            return new JsonResult(packages);
        }

        [HttpGet]
        public async Task<IActionResult> ControllerStatusAsync()
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

        [HttpGet]
        public async Task<IActionResult> PendingPackagesAsync()
        {
            var packages = _jDownloader.QueryCrawledPackagesAsync();
            var list = new List<object>();
            foreach (var p in await packages)
            {
                var size = $"{(p.BytesTotal / (double)1_048_576_000):f1}"; // 1024*1024=1’048’576 * 1000 = 1’048’576’000 ==> ??? why
                // --> JD propably calculates MBs (1024*1024) and then only moves the dot for displaying GB (therefore 1000)
                list.Add(new
                {
                    Name = p.Name,
                    Id = p.UUID.ToString(),
                    Size = size,
                    Unit = "GB"
                });
            }
            return new JsonResult(list);
        }

        [HttpGet]
        public IActionResult UrlHistory()
        {
            var hist = _historyCollection.GetHistory(_urlHistory).Items;
            var values = hist.Select(e => new
            {
                Created = e.Item1.ToUnixTime(),
                Data = e.Item2,
                Id = e.Item3
            }).ToList();
            return new JsonResult(values);
        }

        [HttpPost]
        public async Task<IActionResult> AddUrlsAsync([FromBody] string urls)
        {
            if (ModelState.IsValid)
            {
                var list = urls.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                _historyCollection.GetHistory(_urlHistory).Add(list);
                if (await _jDownloader.AddDownloadLinksAsync(list))
                {
                    return new OkResult();
                }
                return StatusCode(500);
            }
            return new BadRequestResult();
        }

        [HttpPost]
        public async Task<IActionResult> ResubmitUrlsAsync([FromBody] int histId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dllinks = _historyCollection.GetHistory(_urlHistory)[histId];
                    if (await _jDownloader.AddDownloadLinksAsync(dllinks.Item2))
                    {
                        return new OkResult();
                    }
                    return StatusCode(500);
                }
                catch (KeyNotFoundException)
                {
                    return new NotFoundResult();
                }
            }
            return new BadRequestResult();
        }

        [HttpPost]
        public async Task<IActionResult> StartAsync([FromBody] List<long> uuids)
        {
            if (ModelState.IsValid)
            {
                if (await _jDownloader.StartPackageDownloadAsync(uuids))
                {
                    return new OkResult();
                } else
                {
                    return new NotFoundResult();
                }
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> RestartAsync()
        {
            if (ModelState.IsValid)
            {
                var success = await _jDownloader.RestartDownloads();
                return new JsonResult(success);
            }
            return BadRequest();
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

        [HttpDelete]
        public async Task<IActionResult> RemovePendingPackagesAsync([FromBody] List<long> uuids)
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
    }
}

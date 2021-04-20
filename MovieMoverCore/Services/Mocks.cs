using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;

namespace MovieMoverCore.Services
{
    /*
     * _logger = logger;
       _logger.LogInformation("Created instance of " + GetType().ToString());
     * 
     * _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
     */
    public class CacheMock<TOwner, TKey, TElement> : ICache<TOwner, TKey, TElement>
    {
        private ILogger<CacheMock<TOwner, TKey, TElement>> _logger;
        public CacheMock(ILogger<CacheMock<TOwner, TKey, TElement>> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
            EoD = DateTime.Now.AddMinutes(5);
        }
        public DateTime EoD { get; private set; }

        public void Clean()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
        }

        public bool Invalidate(TKey key)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return true;
        }

        public void InvalidateAll()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
        }

        public bool Retrieve(TKey key, out TElement element)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            element = default;
            return false;
        }

        public void UpdateOrAdd(TKey key, TElement element, DateTime validity)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
        }

        public void UpdateOrAdd(TKey key, TElement element, TimeSpan validity)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
        }

        public void UpdateOrAdd(IEnumerable<(TKey key, TElement element)> elements, DateTime validity)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
        }

        public void UpdateOrAdd(IEnumerable<(TKey key, TElement element)> elements, TimeSpan validity)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
        }
    }

    public class DBMock : IDatabase
    {
        private ILogger<DBMock> _logger;
        public DBMock(ILogger<DBMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public Series AddSeries(Series series)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }

        public bool DeleteSeries(Series series)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return true;
        }

        public List<Series> GetSeries()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return new List<Series>();
        }

        public Series GetSeries(int id)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }

        public List<Series> GetSeries(Func<Series, bool> selector)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return new List<Series>();
        }

        public Task SaveSeriesChangesAsync()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.CompletedTask;
        }

        public bool SeriesExists(int id)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return false;
        }

        public bool UpdateSeries(Series series)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return false;
        }
    }

    public class EpGuideMock : IEpGuide
    {
        private ILogger<EpGuideMock> _logger;
        public EpGuideMock(ILogger<EpGuideMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public Task<(List<EpisodeInfo> upcoming, EpisodeInfo nextAdding)> GetEpisodesAsync(EpisodeInfo newestAvailable)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult<(List<EpisodeInfo> upcoming, EpisodeInfo nextAdding)>((new List<EpisodeInfo>(), null));
        }
    }

    public class FilesMoverMock : IFileMover
    {
        private ILogger<FilesMoverMock> _logger;
        public FilesMoverMock(ILogger<FilesMoverMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public Task<bool> AddSubtitleAsync(string fileName, byte[] content, Series series)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(false);
        }

        public FileMoveOperation CreateMoviesMoveOperation(string downloadName)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }

        public FileMoveOperation CreateSeriesMoveOperation(string downloadName, Series series, int? season)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }

        public List<string> GetDownloadEntries()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return new List<string>();
        }

        public List<string> GetSeriesEntries()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return new List<string>();
        }

        public bool ValidateSeriesPath(Series series, bool isNewEntry = false)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return false;
        }
    }

    public class FileWorkerMock : IFileMoveWorker
    {
        private ILogger<FileWorkerMock> _logger;
        public FileWorkerMock(ILogger<FileWorkerMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public bool DismissState(int id)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return false;
        }

        public FileMoveState QueryState(FileMoveOperation fmo)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return FileMoveState.Success;
        }

        public List<FileMoveOperation> QueryStates()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return new List<FileMoveOperation>();
        }

        public FileMoveOperation QueueMoveOperation(string name, string source, string destination, PlexSection plexSection)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }
    }

    public class JDownloaderMock : IJDownloader
    {
        private ILogger<JDownloaderMock> _logger;
        public JDownloaderMock(ILogger<JDownloaderMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public List<JD_FilePackage> LastDownloadStates => new List<JD_FilePackage>();

        public Task<bool> AddDownloadLinksAsync(List<string> links, string packageName = null)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(false);
        }

        public Task<List<JD_CrawledPackage>> QueryCrawledPackagesAsync()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(new List<JD_CrawledPackage>());
        }

        public Task<(bool isDownloading, int speed)> QueryDownloadControllerState()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult((false, 0));
        }

        public Task<List<JD_FilePackage>> QueryDownloadStatesAsync()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(new List<JD_FilePackage>());
        }

        public Task<bool> RemoveDownloadPackageAsync(long uuid)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(false);
        }

        public Task<bool> RemoveDownloadPackageAsync(string downloadPath)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(false);
        }

        public Task<bool> RemoveQueriedDownloadLinksAsync(List<long> uuids)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(false);
        }

        public Task<bool> StartPackageDownloadAsync(List<long> uuids)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(false);
        }

        public void Test()
        {
            throw new InvalidOperationException();
        }
    }

    public class PlexMock : IPlex
    {
        private ILogger<PlexMock> _logger;
        public PlexMock(ILogger<PlexMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public Task<string> GetFilePathOfEpisode(Series series, int season, int episode)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult<string>(null);
        }

        public Task<EpisodeInfo> GetNewestEpisodeAsync(Series series)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult<EpisodeInfo>(null);
        }

        public Task<List<(string id, string name)>> GetSeriesNamesAsync()
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(new List<(string id, string name)>());
        }

        public Task RefreshSectionAsync(PlexSection section, string path = null)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.CompletedTask;
        }

        public bool ResolvePlexId(Series series)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return false;
        }
    }

    public class SeriesVideoSearcherMock : ISeriesVideoSearcher
    {
        private ILogger<SeriesVideoSearcherMock> _logger;
        public SeriesVideoSearcherMock(ILogger<SeriesVideoSearcherMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public bool IsDirectDownloadImplemented => false;

        public Task<List<string>> GetDirectDownloadLinks(Series series, int season, int episode)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult(new List<string>());
        }

        public string GetSearchLink(Series series, int season, int episode)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }
    }

    public class SubtitlesMock : ISubtitles
    {
        private ILogger<SubtitlesMock> _logger;
        public SubtitlesMock(ILogger<SubtitlesMock> logger)
        {
            _logger = logger;
            _logger.LogInformation("Created instance of " + GetType().ToString());
        }
        public bool IsDirectDownloadImplemented => false;

        public Task<string> GetDirectDownloadLinkAsync(Series series, int season, int episode)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return Task.FromResult<string>(null);
        }

        public string GetSearchLink(Series series, int season, int episode)
        {
            _logger.LogInformation($"Called method {MethodBase.GetCurrentMethod()}");
            return null;
        }
    }
}

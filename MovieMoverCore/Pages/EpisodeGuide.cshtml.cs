using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using MovieMoverCore.Models;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages
{
    public class EpisodeGuideModel : PageModel
    {
        public List<(EpisodeInfo newestPlexEpisode, CrawledDownloadPackage nextEpisode)> EpGuide;
        public List<EpisodeInfo> UpcomingEpisodes;

        private readonly IDatabase _db;
        private readonly IPlex _plex;
        private readonly IEpGuide _epGuide;
        private readonly ISubtitles _subtitles;
        private readonly ILogger<EpisodeGuideModel> _logger;
        private readonly ISeriesVideoSearcher _seriesVideoSearcher;

        public EpisodeGuideModel(IDatabase db, IPlex plex, IEpGuide epGuide, ISubtitles subtitles,
            ILogger<EpisodeGuideModel> logger, ISeriesVideoSearcher seriesVideoSearcher)
        {
            _db = db;
            _plex = plex;
            _logger = logger;
            _epGuide = epGuide;
            _subtitles = subtitles;
            _seriesVideoSearcher = seriesVideoSearcher;
        }

        private async Task<(EpisodeInfo newestPlexEpisode, CrawledDownloadPackage nextEpisode, List<EpisodeInfo> upcoming)> GatherInfo(Series series)
        {
            var newestPlex = await _plex.GetNewestEpisodeAsync(series);
            var epInfo = await _epGuide.GetEpisodesAsync(newestPlex);
            var nextInfo = new CrawledDownloadPackage
            {
                EpisodeInfo = epInfo.nextAdding
            };
            if (epInfo.nextAdding != null)
            {
                Task<string> sub = Task.FromResult<string>(null);
                Task<List<string>> video = Task.FromResult<List<string>>(null);
                if (_subtitles.IsDirectDownloadImplemented)
                {
                    sub = _subtitles.GetDirectDownloadLinkAsync(series, epInfo.nextAdding.Season, epInfo.nextAdding.Episode);
                    nextInfo.IsSubtitleDDL = true;
                }
                if (_seriesVideoSearcher.IsDirectDownloadImplemented)
                {
                    video = _seriesVideoSearcher.GetDirectDownloadLinks(series, epInfo.nextAdding.Season, epInfo.nextAdding.Episode);
                    nextInfo.IsSubtitleDDL = true;
                }

                try
                {
                    nextInfo.SubtitleLink = await sub;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, $"Unable to retrieve direct subtitle link for {series.Name} S{epInfo.nextAdding.Season}E{epInfo.nextAdding.Episode}");
                }
                finally
                {
                    if (string.IsNullOrWhiteSpace(nextInfo.SubtitleLink))
                    {
                        nextInfo.IsSubtitleDDL = false;
                        nextInfo.SubtitleLink = _subtitles.GetSearchLink(series, epInfo.nextAdding.Season, epInfo.nextAdding.Episode);
                    }
                }

                try
                {
                    nextInfo.VideoLink = await video;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, $"Unable to retrieve direct video link(s) for {series.Name} S{epInfo.nextAdding.Season}E{epInfo.nextAdding.Episode}");
                }
                finally
                {
                    if (nextInfo.VideoLink == null || !nextInfo.VideoLink.Any())
                    {
                        nextInfo.VideoLink ??= new List<string>();
                        nextInfo.IsVideoDDL = false;
                        nextInfo.VideoLink.Add(_seriesVideoSearcher.GetSearchLink(series, epInfo.nextAdding.Season, epInfo.nextAdding.Episode));
                    }
                }
            }

            return (newestPlex, nextInfo, epInfo.upcoming);
        }

        public async Task OnGetAsync()
        {
            var taskList = new List<Task<(EpisodeInfo newestPlexEpisode, CrawledDownloadPackage nextEpisode, List<EpisodeInfo> upcoming)>>();
            foreach (var s in _db.GetSeries(s => !s.IsFinished && s.SearchNewEpisodes))
            {
                taskList.Add(GatherInfo(s));
            }

            await Task.WhenAll(taskList);
            var results = taskList.Select(t => t.Result);

            UpcomingEpisodes = results.SelectMany(r => r.upcoming).ToList();
            EpGuide = results.Select(r => (r.newestPlexEpisode, r.nextEpisode)) //.OrderBy(r => r.nextEpisode)
                .ToList();
        }
    }
}
using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MovieMoverCore.Services
{
    public interface ISubtitles
    {
        bool IsDirectDownloadImplemented { get; }
        string GetDirectDownloadLink(Series series, int season, int episode);
        string GetSearchLink(Series series, int season, int episode);
    }

    public class Addic7ed : ISubtitles
    {
        public bool IsDirectDownloadImplemented => false;

        private ISettings _settings;

        public Addic7ed(ISettings settings)
        {
            _settings = settings;
        }

        public string GetDirectDownloadLink(Series series, int season, int episode)
        {
            throw new NotImplementedException();
        }

        public string GetSearchLink(Series series, int season, int episode)
        {
            return HttpUtility.UrlEncode(string.Format(_settings.Subtitles_SearchLink, series.SubtitlesName, season.ToString("00"), episode.ToString("00")));
        }
    }
}

using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface ISeriesVideoSearcher
    {
        public bool IsDirectDownloadImplemented { get; }

        Task<List<string>> GetDirectDownloadLinks(Series series, int season, int episode);
        string GetSearchLink(Series series, int season, int episode);
    }

    public class SeriesVideoSearcher : ISeriesVideoSearcher
    {
        public bool IsDirectDownloadImplemented => false;

        private readonly ISettings _settings;

        public SeriesVideoSearcher(ISettings settings)
        {
            _settings = settings;
        }

        public Task<List<string>> GetDirectDownloadLinks(Series series, int season, int episode)
        {
            throw new NotImplementedException();
        }

        public string GetSearchLink(Series series, int season, int episode)
        {
            return string.Format(_settings.DL_Series_SearchLink, series.VideoSearch, season.ToString("00"), episode.ToString("00"));
        }
    }
}

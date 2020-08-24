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

        public Task<List<string>> GetDirectDownloadLinks(Series series, int season, int episode)
        {
            throw new NotImplementedException();
        }

        public string GetSearchLink(Series series, int season, int episode)
        {
            return "nope";
        }
    }
}

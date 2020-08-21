using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface IEpGuide
    {
        Task<(EpisodeInfo nextAiring, EpisodeInfo nextAdding)> GetEpisodesAsync(EpisodeInfo newestAvailable);
    }

    public class EpGuidesCom : IEpGuide
    {
        private class EpCsv
        {
            // number,season,episode,airdate,title,tvmaze link
            public int Number { get; set; }
            public int Season { get; set; }
            public int Episode { get; set; }
            public string Airdata { get; set; }
            public string titel { get; set; }
            public string TvmazeLink { get; set; }
        }

        private ISettings _settings;
        private ILogger<EpGuidesCom> _logger;

        public EpGuidesCom(ISettings settings, ILogger<EpGuidesCom> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task<(EpisodeInfo nextAiring, EpisodeInfo nextAdding)> GetEpisodesAsync(EpisodeInfo newestAvailable)
        {
            var wc = new WebClient();
            var mainPageData = await wc.DownloadStringTaskAsync(string.Format(_settings.EpGuide_SearchLink, newestAvailable.Series.EpGuidesName));
            var lineCsvLink = mainPageData.Split(Environment.NewLine).FirstOrDefault(l => l.Contains("exportToCSVmaze"));
            // use csv plugin -> csvhelper: https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records
            throw new NotImplementedException();
        }
    }
}

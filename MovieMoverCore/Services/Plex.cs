using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    
    public interface IPlex
    {
        EpisodeInfo GetNewestEpisode(Series series);
        string GetSettingsInfo();
    }

    public class Plex : IPlex
    {
        private readonly ISettings _settings;

        public Plex(ISettings settings)
        {
            _settings = settings;
        }

        public string GetSettingsInfo()
        {
            return $"Url:\t{_settings.Plex_BaseUrl}\nToken:\t{_settings.Plex_ApiToken}";
        }

        public EpisodeInfo GetNewestEpisode(Series series)
        {
            return new EpisodeInfo
            {
                AirDate = DateTime.Now.Date,
                Episode = 42,
                Season = 10,
                Series = series,
                Title = "1337_Leet"
            };
        }
    }
}

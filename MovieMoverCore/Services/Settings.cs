using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface ISettings
    {
        string Plex_BaseUrl { get; }
        string Plex_ApiToken { get; }
        string Plex_MoviesSectionId { get; }
        string Plex_SeriesSectionId { get; }
    }

    public class Settings : ISettings
    {
        public string Plex_BaseUrl { get; private set; }

        public string Plex_ApiToken { get; private set; }

        public string Plex_MoviesSectionId { get; private set; }

        public string Plex_SeriesSectionId { get; private set; }

        public Settings()
        {
            Plex_BaseUrl = Environment.GetEnvironmentVariable("PLEX_BaseUrl");
            Plex_ApiToken = Environment.GetEnvironmentVariable("PLEX_ApiToken");
#if DEBUG
            if (Plex_ApiToken == null && File.Exists("/secrets/plex_apitoken.txt"))
            {
                Plex_ApiToken = File.ReadAllText("/secrets/plex_apitoken.txt");
            }
#endif
            Plex_MoviesSectionId = Environment.GetEnvironmentVariable("PLEX_MoviesSection");
            Plex_SeriesSectionId = Environment.GetEnvironmentVariable("PLEX_SeriesSection");
        }
    }
}

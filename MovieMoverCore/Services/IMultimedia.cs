using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface IMultimediaServerManager
    {
        bool IsMultimediaManagerEnabled { get; }
        Task InformUpdatedFilesAsync(MultimediaType type, string path);
    }

    public interface IMultimediaMetadataProvider
    {
        Task<EpisodeInfo> GetNewestEpisodeAsync(Series series);
        Task<List<(string id, string name)>> GetSeriesNamesAsync();
        Task<string> GetFilePathOfEpisode(Series series, int season, int episode);
        bool ResolveProviderId(Series series);
    }
    public interface IMultimediaServerManagerCollection
    {
        public IList<IMultimediaServerManager> Managers { get; }
    }

    public class MultimediaServerManagerCollection : IMultimediaServerManagerCollection
    {
        public IList<IMultimediaServerManager> Managers {get; private set; }

        public MultimediaServerManagerCollection(ISettings settings, ILoggerFactory loggerFactory)
        {
            var plex_logger = loggerFactory.CreateLogger<Plex>();
            var plex = new Plex(settings, plex_logger);

            var jellyfin_logger = loggerFactory.CreateLogger<Jellyfin>();
            var jellyfin = new Jellyfin(settings, jellyfin_logger);


            Managers = new List<IMultimediaServerManager>();
            if (plex.IsMultimediaManagerEnabled)
            {
                Managers.Add(plex);
            }
            if (jellyfin.IsMultimediaManagerEnabled)
            {
                Managers.Add(jellyfin);
            }
        }
    }
}

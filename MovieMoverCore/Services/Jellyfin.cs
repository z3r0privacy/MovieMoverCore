using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public class Jellyfin : IMultimediaServerManager
    {
        private readonly ISettings _settings;
        private readonly ILogger<Jellyfin> _logger;
        
        public bool IsMultimediaManagerEnabled => true;

        public Jellyfin(ISettings settings, ILogger<Jellyfin> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task InformUpdatedFilesAsync(MultimediaType type, string path)
        {
            var bodyData = """
                {
                    "Updates": [
                        {
                            "Path": "{0}",
                            "UpdateType": "Created"
                        }
                    ]
                }
                """.Replace("{0}", path);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"MediaBrowser Token=\"{_settings.Jellyfin_ApiToken}\"");
            using StringContent jsonContent = new StringContent(bodyData, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync($"{_settings.Jellyfin_BaseUrl}Library/Media/Updated", jsonContent);
            _logger.LogInformation($"Updating jellyfin path '{path}'. Result: {result.StatusCode}");
        }
    }
}

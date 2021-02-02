using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface ISettings
    {
        string AppDataDirectory { get; }

        string Plex_BaseUrl { get; }
        string Plex_ApiToken { get; }
        string Plex_MoviesSectionId { get; }
        string Plex_SeriesSectionId { get; }

        public string Files_DownloadsPath { get; }
        public string Files_MoviesPath { get; }
        public string Files_SeriesPath { get; }
        public int Files_KeepSuccess { get; }

        public string Subtitles_SearchLink { get; }
        public string EpGuide_SearchLink { get; }
        public string DL_Series_SearchLink { get; }


        public string JD_Email { get; }
        public string JD_Password { get;}
        public string JD_ApiPath { get; }
        public string JD_My_ApiPath { get; }
        public bool JD_Use_Direct { get; }
        public string JD_PreferredClient { get; }
        public int JD_MaxRefreshInterval { get; }

        void RegisterCertificateValidationCallback(RemoteCertificateValidationCallback callBack);
    }

    public class Settings : ISettings
    {
        public string Plex_BaseUrl { get; private set; }

        public string Plex_ApiToken { get; private set; }

        public string Plex_MoviesSectionId { get; private set; }

        public string Plex_SeriesSectionId { get; private set; }


        public string Files_DownloadsPath { get; private set; }
        public string Files_MoviesPath { get; private set; }
        public string Files_SeriesPath { get; private set; }
        public int Files_KeepSuccess { get; private set; }


        public string Subtitles_SearchLink { get; private set; }


        public string EpGuide_SearchLink { get; private set; }


        public string DL_Series_SearchLink { get; private set; }


        public string JD_Email { get; private set; }
        public string JD_Password { get; private set; }
        public string JD_ApiPath { get; private set; }
        public string JD_PreferredClient { get; private set; }
        public string JD_My_ApiPath { get; private set; }
        public bool JD_Use_Direct { get; private set; }
        public int JD_MaxRefreshInterval { get; private set; }


        public string AppDataDirectory => "/appdata";


        private List<RemoteCertificateValidationCallback> _customValidators;

        private ILogger<Settings> _logger;

        public Settings(ILogger<Settings> logger)
        {
            _logger = logger;
            _customValidators = new List<RemoteCertificateValidationCallback>();
            ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                {
                    return true;
                }

                return _customValidators.Any(cb => cb(sender, certificate, chain, sslPolicyErrors));
            };

            Plex_BaseUrl = Environment.GetEnvironmentVariable("PLEX_BaseUrl");
            if (!Plex_BaseUrl.EndsWith("/"))
            {
                Plex_BaseUrl += "/";
            }
            Plex_ApiToken = Environment.GetEnvironmentVariable("PLEX_ApiToken");
#if DEBUG
            if (Plex_ApiToken == null && File.Exists("/secrets/plex_apitoken.txt"))
            {
                Plex_ApiToken = File.ReadAllText("/secrets/plex_apitoken.txt");
                _logger.LogDebug("Reading Plex-Token from secrets folder");
            }
#endif
            Plex_MoviesSectionId = Environment.GetEnvironmentVariable("PLEX_MoviesSection");
            Plex_SeriesSectionId = Environment.GetEnvironmentVariable("PLEX_SeriesSection");


            Files_DownloadsPath = Path.Combine("/data", Environment.GetEnvironmentVariable("FILES_Downloads"));
            Files_MoviesPath = Path.Combine("/data", Environment.GetEnvironmentVariable("FILES_Movies"));
            Files_SeriesPath = Path.Combine("/data", Environment.GetEnvironmentVariable("FILES_Series"));
            Files_KeepSuccess = int.Parse(Environment.GetEnvironmentVariable("FILES_KeepSuccess"));

            Subtitles_SearchLink = Environment.GetEnvironmentVariable("SUBS_SearchLink");

            EpGuide_SearchLink = Environment.GetEnvironmentVariable("EPGUIDE_SearchLink");

            DL_Series_SearchLink = Environment.GetEnvironmentVariable("DL_Series_SearchLink");

            JD_ApiPath = Environment.GetEnvironmentVariable("JD_ApiPath");
            JD_My_ApiPath = Environment.GetEnvironmentVariable("JD_My_ApiPath");
            JD_Use_Direct = string.Equals(Environment.GetEnvironmentVariable("JD_Method"), "direct", StringComparison.OrdinalIgnoreCase);
            JD_Email = Environment.GetEnvironmentVariable("JD_Email");
            JD_Password = Environment.GetEnvironmentVariable("JD_Password");
            JD_PreferredClient = Environment.GetEnvironmentVariable("JD_PreferredClient");
            JD_MaxRefreshInterval = int.Parse(Environment.GetEnvironmentVariable("JD_MaxRefreshInterval"));
#if DEBUG
            if (JD_Password == null && File.Exists("/secrets/jd_email.txt"))
            {
                JD_Email = File.ReadAllText("/secrets/jd_email.txt");
                _logger.LogDebug("Reading JD email from secrets folder");
            }
            if (JD_Password == null && File.Exists("/secrets/jd_password.txt"))
            {
                JD_Password = File.ReadAllText("/secrets/jd_password.txt");
                _logger.LogDebug("Reading JD pwd from secrets folder");
            }
            if (JD_ApiPath == null && File.Exists("/secrets/jd_localpath.txt"))
            {
                JD_ApiPath = File.ReadAllText("/secrets/jd_localpath.txt");
                _logger.LogDebug("Reading JD local Api Path from secrets folder");
            }
#endif

            ValidateSettings();
        }

        private void ValidateSettings()
        {

        }

        public void RegisterCertificateValidationCallback(RemoteCertificateValidationCallback callBack)
        {
            _customValidators.Add(callBack);
            _logger.LogDebug("A new callback for certificate validation callbacks has been added");
        }
    }
}

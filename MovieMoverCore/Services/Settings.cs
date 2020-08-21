﻿using Microsoft.Extensions.Logging;
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

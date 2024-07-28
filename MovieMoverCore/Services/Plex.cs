﻿using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace MovieMoverCore.Services
{
    
    //public interface IPlex
    //{
    //    Task<EpisodeInfo> GetNewestEpisodeAsync(Series series);
    //    Task RefreshSectionAsync(MultimediaType section, string path = null);
    //    Task<List<(string id, string name)>> GetSeriesNamesAsync();
    //    Task<string> GetFilePathOfEpisode(Series series, int season, int episode);
    //    bool ResolvePlexId(Series series);
    //}

    public class Plex : IMultimediaMetadataProvider, IMultimediaServerManager
    {
        private readonly ISettings _settings;
        private readonly ILogger<Plex> _logger;
        private readonly HttpClientHandler _httpClientHandler;

        public bool IsMultimediaManagerEnabled => true;

        public Plex(ISettings settings, ILogger<Plex> logger)
        {
            _logger = logger;
            _settings = settings;
            _httpClientHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = CertificateCheck
            };
        }

        private bool CertificateCheck(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            if (sender is HttpRequestMessage hrm)
            {
                if (hrm.RequestUri.AbsoluteUri.StartsWith(_settings.Plex_BaseUrl))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetSettingsInfo()
        {
            return $"Url:\t{_settings.Plex_BaseUrl}\nToken:\t{_settings.Plex_ApiToken}";
        }

        public async Task<EpisodeInfo> GetNewestEpisodeAsync(Series series)
        {
            //var wc = new WebClient();
            //var query = $"{_settings.Plex_BaseUrl}library/metadata/{series.PlexId}/children?X-Plex-Token={{0}}";
            //_logger.LogDebug($"Query season data for {series.Name} using {query}", "***");
            //var seasonsData = await wc.DownloadStringTaskAsync(string.Format(query, _settings.Plex_ApiToken));

            //var xml = new XmlDocument();
            //xml.LoadXml(seasonsData);
            //var newestSeasonKey = xml.DocumentElement.ChildNodes.Cast<XmlNode>().Last().Attributes["ratingKey"].InnerText;

            //query = $"{_settings.Plex_BaseUrl}library/metadata/{newestSeasonKey}/children?X-Plex-Token={{0}}";
            //_logger.LogDebug($"Query episode data for {series.Name} using {query}", "***");
            //var epsData = await wc.DownloadStringTaskAsync(string.Format(query, _settings.Plex_ApiToken));

            var hc = new HttpClient(_httpClientHandler);
            // var wc = new WebClient();
            var query = $"{_settings.Plex_BaseUrl}library/metadata/{series.MetadataProviderId}/allLeaves?X-Plex-Token={{0}}";
            _logger.LogDebug($"Query episode data for {series.Name} using {query}", "***");
            var response = await hc.GetAsync(string.Format(query, _settings.Plex_ApiToken));
            var epsData = await response.Content.ReadAsStringAsync();
            //var epsData = await wc.DownloadStringTaskAsync(string.Format(query, _settings.Plex_ApiToken));

            var xml = new XmlDocument();
            xml.LoadXml(epsData);
            var ep = xml.DocumentElement.ChildNodes.Cast<XmlNode>().Last();
            var released = ep.Attributes["originallyAvailableAt"];
            string[] rawdate = null;
            if (released != null)
            {
                rawdate = released.InnerText.Split("-");
            }
            
            return new EpisodeInfo
            {
                AirDate = (rawdate != null) ? new DateTime(int.Parse(rawdate[0]), int.Parse(rawdate[1]), int.Parse(rawdate[2])) : DateTime.MinValue,
                Episode = int.Parse(ep.Attributes["index"].InnerText),
                Season = int.Parse(ep.Attributes["parentIndex"].InnerText),
                Series = series,
                Title = ep.Attributes["title"].InnerText
            };
        }

        public async Task InformUpdatedFilesAsync(MultimediaType section, string path = null)
        {
            string refreshId;
            if (section == MultimediaType.Movies)
            {
                refreshId = _settings.Plex_MoviesSectionId;
            } else if (section == MultimediaType.Series)
            {
                refreshId = _settings.Plex_SeriesSectionId;
            } else
            {
                throw new ArgumentException();
            }

            var query = $"{_settings.Plex_BaseUrl}library/sections/{refreshId}/refresh?X-Plex-Token={{0}}";
            if (path != null)
            {
                query += $"&path={HttpUtility.UrlEncode(path)}";
            }

            _logger.LogDebug("Refreshing a section using " + query, "***");

            var hc = new HttpClient(_httpClientHandler);
            await hc.GetAsync(string.Format(query, _settings.Plex_ApiToken));
            //var wc = new WebClient();
            //await wc.DownloadStringTaskAsync(string.Format(query, _settings.Plex_ApiToken));
        }

        public async Task<List<(string id, string name)>> GetSeriesNamesAsync()
        {
            var hc = new HttpClient(_httpClientHandler);
            //var wc = new WebClient();
            var queryString = $"{_settings.Plex_BaseUrl}library/sections/{_settings.Plex_SeriesSectionId}/all?X-Plex-Token={{0}}";
            _logger.LogDebug("Querying series from Plex using " + queryString, "***");

            var response = await hc.GetAsync(string.Format(queryString, _settings.Plex_ApiToken));
            var data = await response.Content.ReadAsStringAsync();
            //var data = await wc.DownloadStringTaskAsync(
            //    string.Format(queryString, _settings.Plex_ApiToken)
            //    );
            var xml = new XmlDocument();
            xml.LoadXml(data);
            return xml.DocumentElement.ChildNodes.Cast<XmlNode>().Select(n => 
                (n.Attributes["ratingKey"].InnerText, n.Attributes["title"].InnerText)
                ).ToList();
        }

        public async Task<string> GetFilePathOfEpisode(Series series, int season, int episode)
        {
            var hc = new HttpClient(_httpClientHandler);
            //var wc = new WebClient();
            var query = $"{_settings.Plex_BaseUrl}library/metadata/{series.MetadataProviderId}/allLeaves?X-Plex-Token={{0}}";
            _logger.LogDebug($"Query episode data for {series.Name} using {query}", "***");
            var response = await hc.GetAsync(string.Format(query, _settings.Plex_ApiToken));
            var epData = await response.Content.ReadAsStringAsync();
            //var epData = await wc.DownloadStringTaskAsync(string.Format(query, _settings.Plex_ApiToken));

            var xml = new XmlDocument();
            xml.LoadXml(epData);
            var node = xml.DocumentElement.ChildNodes.Cast<XmlNode>().FirstOrDefault(c => c.Attributes["parentIndex"].InnerText == season.ToString() && c.Attributes["index"].InnerText == episode.ToString());
            if (node == null)
            {
                _logger.LogWarning($"Could not find episode S{season}E{episode} of series {series.Name}");
                return null;
            }

            return node.SelectSingleNode("Media/Part").Attributes["file"].InnerText;
        }

        public bool ResolveProviderId(Series series)
        {
            var plexData = GetSeriesNamesAsync().Result;

            var entry = plexData.FirstOrDefault(e => string.Equals(e.name, series.Name, StringComparison.CurrentCultureIgnoreCase));
            if (entry == default)
            {
                return false;
            }

            series.Name = entry.name;
            series.MetadataProviderId = entry.id;

            return true;
        }
    }
}

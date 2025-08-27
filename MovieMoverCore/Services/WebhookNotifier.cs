using MovieMoverCore.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Http;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Net.Http.Json;
using System.Text;
using Azure.Core.Pipeline;

namespace MovieMoverCore.Services
{
    class WebhookInstance
    {
        [JsonPropertyName("requestMethod")]
        public string RequestMethod { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("body")]
        public dynamic Body { get; set; }
        [JsonPropertyName("headers")]
        public Dictionary<string,string> Headers { get; set; }
    }

    public class WebhookNotifier : IMultimediaServerManager
    {
        public bool IsMultimediaManagerEnabled => true;

        private readonly List<WebhookInstance> _instances = [];

        public WebhookNotifier(ISettings settings)
        {
            var file_path = Path.Combine(settings.AppDataDirectory, "webhooks.json");
            if (File.Exists(file_path))
            {
                var hooks = JsonSerializer.Deserialize<List<WebhookInstance>>(File.ReadAllText(file_path));
                _instances.AddRange(hooks);
            }
        }

        private static string ReplacePlaceHolders(string template, MultimediaType type, string path)
        {
            return template.Replace("[[path]]", path).Replace("[[type_str]]", type.ToString()).Replace("[[type_int]]", ((int)type).ToString());
        }

        public async Task InformUpdatedFilesAsync(MultimediaType type, string path)
        {
            foreach(var hook in _instances)
            {
                var url = ReplacePlaceHolders(hook.Url, type, path);

                var hc = new HttpClient();
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url)
                };

                switch (hook.RequestMethod.ToLower()) {
                    case "get":
                        request.Method = HttpMethod.Get;
                        break;
                    case "post":
                        request.Method = HttpMethod.Post;
                        break;
                    case "put":
                        request.Method = HttpMethod.Put;
                        break;
                    default:
                        throw new NotImplementedException($"Request type '{hook.RequestMethod}' is not implemented");
                }

                foreach(var header in hook.Headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                if (request.Method != HttpMethod.Get)
                {
                    var json_body = JsonSerializer.Serialize(hook.Body);
                    json_body = ReplacePlaceHolders(json_body, type, path);
                    request.Content = new StringContent(json_body, Encoding.UTF8, "application/json");
                }

                await hc.SendAsync(request);
            }
        }
    }
}

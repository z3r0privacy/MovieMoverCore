using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using MovieMoverCore.Helpers;
using MovieMoverCore.Models;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace MovieMoverCore.Services
{
    public enum JDState
    {
        UnInit, Ready, TimedOut, NoConnection, Overload, NoDevice, Error
    }
    public interface IJDownloader
    {
        void Test();
    }

    public class JDownloader : IJDownloader
    {

        private readonly ISettings _settings;
        private readonly ILogger<JDownloader> _logger;

        private SHA256 _sha256Alg;

        private string _sessionToken;
        private string _regainToken;
        private byte[] _deviceSecret;
        private byte[] _serverEncryptionToken;
        private byte[] _deviceEncryptionToken;
        private string _selectedDeviceId;
        private string _appKey;

        private int _rid = 0;
        private int RID => Interlocked.Increment(ref _rid);

        private Exception _lastException;
        
        public JDownloader(ISettings settings, ILogger<JDownloader> logger)
        {
            _settings = settings;
            _logger = logger;
            _sha256Alg = SHA256.Create();
            _appKey = "MovieMover";
        }

        public void Test()
        {
            JDState state;
            Server_LoginAsync().Wait();
            var devs = Server_ListDevicesAsync().Result;
            _selectedDeviceId = devs.Item2[0].Id;

            var pkgs = Device_QueryDownloadPackagesAsync().Result;
            foreach ( var p in pkgs.Item2)
            {
                var icon = Device_GetIconAsync(p.StatusIconKey).Result;
            }
            foreach (var p in pkgs.Item2) //.Where(pk => pk.PackageState == JD_PackageState.Finished))
            {
                List<JD_ArchiveStatus> archiveStatus;
                (state, archiveStatus) = Device_GetArchiveInfoAsync(p).Result;
                if (archiveStatus.Any(s => s.ControllerStatus != JD_ControllerStatus.NA))
                {
                    p.IsExtracting = true;
                }
            }


            var links = new List<string>()
            {
                //"https://ouo.io/3R7YSl",
                //"https://ouo.io/3Z76CC",
                //"https://ouo.io/eZmm7ek",
                //"https://ouo.io/I8ikEk",
                //"https://ouo.io/7f8CYS",
                //"https://ouo.io/oSMcnDo",
                //"https://ouo.io/zFCPTT",
                //"https://ouo.io/Z6xPIM",
                //"https://ouo.io/9ku4yj",
                //"https://ouo.io/lt0BTM",
                //"https://ouo.io/WWlXd3",
                //"https://ouo.io/e7MhVF",
                //"https://ouo.io/tyTbTE",
                //"https://ouo.io/aYPJZtg",
                //"https://ouo.io/4Hc1my",
                //"https://ouo.io/ki5i1m",
                //"https://ouo.io/vETHWZ",
            };
            (var _, var id) = Device_AddLinksAsync("Test", links).Result;

            for (int i = 0; i < 3; i++)
            {
                Device_QueryLinkCrawlerRequestAsync(id).Wait();
            }

            var crawled = Device_QueryCrawledPackagesAsync().Result;

            //Device_MoveToDownloadsAsync(crawled.Item2.First(i => i.Name == "Test")).Wait();
            //Server_Reconnect().Wait();
            //Server_ListDevices().Wait();
        }

        // Here, the interface implementation is provided
        #region public callable methods
        private void ActionTemplate()
        {
            // check state (if state wrong, try to reach - otherwise directly fail)
            // try to exec
            // if fail, try to repair (general repair method using states)
            // retry
        }
        #endregion


        // Here, error handling and state handling is provided
        #region error and state handling

        #endregion


        // Here, helper methods for en/decryption, signatures, etc are provided
        #region HelperMethods

        public static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private byte[] CalcSecret(byte[] secret, string token)
        {
            var tokenBytes = StringToByteArray(token);
            var inData = secret.Concat(tokenBytes).ToArray();
            return _sha256Alg.ComputeHash(inData);
        }

        private byte[] CalcLoginSecret(string content)
        {
            return _sha256Alg.ComputeHash(Encoding.UTF8.GetBytes(content));
        }

        private string CreateSignature(string content, byte[] secret)
        {
            using var hmacAlg = new HMACSHA256(secret);
            var hash = hmacAlg.ComputeHash(Encoding.UTF8.GetBytes(content));
            var retStr = "";
            foreach (var b in hash)
            {
                retStr += b.ToString("x2");
            }
            return retStr;
        }

        private string Decrypt(byte[] content, byte[] secret)
        {
            var iv = secret[0..16];
            var key = secret[16..];
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.IV = iv;
            aes.Key = key;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            var decryptor = aes.CreateDecryptor(key, iv);

            using var ms = new MemoryStream(content);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        private string Encrypt(string content, byte[] secret)
        {
            var iv = secret[0..16];
            var key = secret[16..];
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.IV = iv;
            aes.Key = key;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            var encryptor = aes.CreateEncryptor(key, iv);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(content);
            }
            ms.Flush();
            return Convert.ToBase64String(ms.ToArray());
        }

        private string CreateQuery(string query, byte[] token, bool hasOnlySigParam = false)
        {
            var sig = CreateSignature(query, token);
            return $"{_settings.JD_ApiPath}{query}{(hasOnlySigParam ? "?" : "&")}signature={sig}";
        }

        private async Task<T> DecryptResponseAsync<T>(Task<HttpResponseMessage> httpResponse, byte[] decKey, bool returnUnmodified = false)
        {
            var response = await httpResponse;
            if (returnUnmodified)
            {
                if (!customUnmodifiedHandlers.ContainsKey(typeof(T)))
                {
                    throw new InvalidOperationException("No handler for type " + typeof(T).ToString());
                }
                return (T) await customUnmodifiedHandlers[typeof(T)](response, decKey);
            }

            var encBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                // these errors are unencrypted
                var err = JsonSerializer.Deserialize<JD_Error>(encBody);
                throw new JDException(err);
            }
            var rawString = Decrypt(Convert.FromBase64String(encBody), decKey);
            var opt = new JsonSerializerOptions();
            //opt.Converters.Add(new JsonConverterNullableJDPriority());
            if (!response.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<JD_Error>(rawString);
                throw new JDException(err);
            }
            return JsonSerializer.Deserialize<T>(rawString, opt);
        }
        #endregion


        // Here, API calls are implemented
        #region api calls
        private async Task<JDState> Server_LoginAsync()
        {
            try
            {
                var loginSecret = CalcLoginSecret(_settings.JD_Email.ToLower() + _settings.JD_Password + "server");
                _deviceSecret = CalcLoginSecret(_settings.JD_Email.ToLower() + _settings.JD_Password + "device");
                var rid = RID;
                var query = $"/my/connect?email={_settings.JD_Email.ToLower()}&appkey={_appKey}&rid={rid}";
                var sig = CreateSignature(query, loginSecret);
                query = _settings.JD_ApiPath + query + "&signature=" + sig;

                var response = await DecryptResponseAsync<JD_LoginResponse>(new HttpClient().PostAsync(query, null), loginSecret);
                if (response.Rid != rid)
                {
                    throw new InvalidDataException("The rid is not the expected one.");
                }
                _sessionToken = response.SessionToken;
                _regainToken = response.RegainToken;

                _deviceEncryptionToken = CalcSecret(_deviceSecret, _sessionToken);
                _serverEncryptionToken = CalcSecret(loginSecret, _sessionToken);

                return JDState.Ready;
            }
            catch (Exception ex)
            {
                _lastException = ex;
                _logger.LogWarning(ex, "Could not log in to JDownloader API");
                return JDState.Error;
            }
        }

        private async Task<JDState> Server_Reconnect()
        {
            if (_sessionToken == null || _regainToken == null)
            {
                _lastException = new InvalidOperationException("session and/or regaintoken not set, unable to reconnect");
                return JDState.Error;
            }

            try
            {
                var rid = RID;
                var query = $"/my/reconnect?sessiontoken={_sessionToken}&regaintoken={_regainToken}&rid={rid}";
                query = CreateQuery(query, _serverEncryptionToken);

                var response = await DecryptResponseAsync<JD_LoginResponse>(new HttpClient().PostAsync(query, null), _serverEncryptionToken);
                if (response.Rid != rid)
                {
                    throw new InvalidDataException("The rid is not the expected one.");
                }

                _sessionToken = response.SessionToken;
                _regainToken = response.RegainToken;

                _deviceEncryptionToken = CalcSecret(_deviceSecret, _sessionToken);
                _serverEncryptionToken = CalcSecret(_serverEncryptionToken, _sessionToken);

                return JDState.Ready;
            } catch (Exception ex)
            {
                _lastException = ex;
                return JDState.Error;
            }
        }

        private async Task<(JDState, List<JD_Device>)> Server_ListDevicesAsync()
        {
            try
            {
                var rid = RID;
                var query = $"/my/listdevices?sessiontoken={_sessionToken}&rid={rid}";
                query = CreateQuery(query, _serverEncryptionToken);
                var response = await DecryptResponseAsync<JD_ListDevice_Response>(new HttpClient().PostAsync(query, null), _serverEncryptionToken);
                if (response.Rid != rid)
                {
                    throw new InvalidOperationException("The rid is not the expected one.");
                }

                return (JDState.Ready, response.Devices);
            }
            catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, null);
            }
        }


        private Dictionary<Type, Func<HttpResponseMessage, byte[], Task<object>>> customUnmodifiedHandlers = new Dictionary<Type, Func<HttpResponseMessage, byte[], Task<object>>>()
        {
            {typeof(JD_Response<byte[]>), async (msg, key) =>
            {
                return new JD_Response<byte[]>{ Data = await msg.Content.ReadAsByteArrayAsync() };
            }
            },
            {typeof(JD_Response<string>), async (msg, key) =>
            {
                return new JD_Response<string>{ Data = await msg.Content.ReadAsStringAsync() };
            }
            },
            {typeof(JD_Response<Stream>), async (msg, key) =>
            {
                return new JD_Response<Stream>{ Data = await msg.Content.ReadAsStreamAsync() };
            }
            }
        };

        private async Task<T> CallDeviceAsync<T>(string action, params object[] queryParams) => await CallDeviceAsync<T>(action, returnUnmodified: false, queryParams);
        private async Task<T> CallDeviceAsync<T>(string action, bool returnUnmodified, params object[] queryParams)
        {
            var postData = new JD_Request
            {
                Rid = RID,
                Url = action
            };
            postData.Params.AddRange(
                queryParams.Select(o => JsonSerializer.Serialize(o, new JsonSerializerOptions() { IgnoreNullValues = true }))
                );

            var query = $"/t_{_sessionToken}_{_selectedDeviceId}{postData.Url}";
            query = _settings.JD_ApiPath + query; //CreateQuery(query, _deviceEncryptionToken, true);

            var body = JsonSerializer.Serialize(postData, new JsonSerializerOptions
            {
                IgnoreNullValues = true
            });
            var bodyEnc = Encrypt(body, _deviceEncryptionToken);


            var cnt = new StringContent(bodyEnc, Encoding.UTF8, "application/json");
            var response = await DecryptResponseAsync<JD_Response<T>>(new HttpClient().PostAsync(query, cnt), _deviceEncryptionToken, returnUnmodified);
            if (response.Rid != postData.Rid)
            {
                throw new InvalidDataException("The rid is not the expected one.");
            }
            return response.Data;
        }

        private async Task<(JDState, List<JD_FilePackage>)> Device_QueryDownloadPackagesAsync()
        {
            try
            {
                var param = new JD_QueryDevices_Request_Params();

                var response = await CallDeviceAsync<List<JD_FilePackage>>("/downloadsV2/queryPackages", param);

                return (JDState.Ready, response);
            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, null);
            }
        }


        private async Task<(JDState, List<JD_ArchiveStatus>)> Device_GetArchiveInfoAsync(JD_FilePackage package)
        {
            try
            {
                var param = new long[] { package.UUID };
                var response = await CallDeviceAsync<List<JD_ArchiveStatus>>("/extraction/getArchiveInfo", new object[0], param);

                return (JDState.Ready, response);
            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, null);
            }
        }


        private async Task<(JDState, long)> Device_AddLinksAsync(string name, List<string> links)
        {
            try
            {
                var requestData = new JD_AddLinks_Request_Params(name, links);
                
                var response = await CallDeviceAsync<JD_AddLinks_Response>("/linkgrabberv2/addLinks", requestData);
                return (JDState.Ready, response.Id);
            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, -1);
            }
        }

        private async Task<(JDState state, JD_JobLinkCrawler)> Device_QueryLinkCrawlerRequestAsync(long jobId)
        {
            try
            {
                var param = new JD_QueryLinkCrawler_Request_Params(jobId);

                var response = await CallDeviceAsync<List<JD_JobLinkCrawler>>("/linkgrabberv2/queryLinkCrawlerJobs", param);

                return (JDState.Ready, response[0]);

            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, null);
            }
        }

        private async Task<(JDState, List<JD_CrawledPackage>)> Device_QueryCrawledPackagesAsync()
        {
            try
            {
                var param = new JD_CrawledPackageQuery_Request_Params();

                var response = await CallDeviceAsync<List<JD_CrawledPackage>>("/linkgrabberv2/queryPackages", param);

                return (JDState.Ready, response);
            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, null);
            }
        }

        private async Task<JDState> Device_MoveToDownloadsAsync(JD_CrawledPackage package)
        {
            try
            {
                await CallDeviceAsync<dynamic>("/linkgrabberv2/moveToDownloadlist", new object[0], new long[] { package.UUID });
                return JDState.Ready;
            } catch (Exception ex)
            {
                _lastException = ex;
                return JDState.Error;
            }
        }
        
        private async Task<(JDState, dynamic)> Device_GetIconAsync(string iconKey)
        {
            try
            {
                var resp = await CallDeviceAsync<byte[]>("/contentV2/getIcon", returnUnmodified: true, iconKey, 100);
                File.WriteAllBytes(@"C:\temp\test.png", resp);
                return (JDState.Ready, resp);
            }
            catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, null);
            }
        }
        #endregion
    }
}

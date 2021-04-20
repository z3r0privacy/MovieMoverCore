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
using System.Net;
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
        NotStarted, Ready, TimedOut, NoConnection, Overload, NoDevice, Error
    }
    public interface IJDownloader
    {
        void Test();

        List<JD_FilePackage> LastDownloadStates { get; }
        
        Task<List<JD_FilePackage>> QueryDownloadStatesAsync();
        Task<bool> RemoveDownloadPackageAsync(long uuid);
        Task<bool> RemoveDownloadPackageAsync(string downloadPath);
        Task<List<JD_CrawledPackage>> QueryCrawledPackagesAsync();
        Task<bool> StartPackageDownloadAsync(List<long> uuids);
        Task<bool> AddDownloadLinksAsync(List<string> links, string packageName = null);
        Task<bool> RemoveQueriedDownloadLinksAsync(List<long> uuids);
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

        private string _apiBase;

        private DateTime? _timeOut = null;

        private int _rid = 0;
        private int RID => Interlocked.Increment(ref _rid);

        private Exception _lastException;
        private JDState _CurrentState
        {
            get => _currentState;
            set => _currentState = value;
        }
        private volatile JDState _currentState;

        public List<JD_FilePackage> LastDownloadStates { get; private set; }

        public JDownloader(ISettings settings, ILogger<JDownloader> logger)
        {
            _settings = settings;
            _logger = logger;
            _sha256Alg = SHA256.Create();
            _appKey = "MovieMover";
            _CurrentState = JDState.NotStarted;
            LastDownloadStates = new List<JD_FilePackage>();
            _apiBase = _settings.JD_Use_Direct ? _settings.JD_ApiPath : _settings.JD_My_ApiPath;
        }

        public void Test()
        {
            return;
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

        private static object _requireStateLock = new object();
        private bool IsReady()
        {
            lock (_requireStateLock)
            {
                // NotStarted, Ready, TimedOut, NoConnection, Overload, NoDevice, Error
                JDState lastState;
                bool res;

                if (_timeOut.HasValue)
                {
                    if (DateTime.Now < _timeOut.Value)
                    {
                        _lastException = new RecoveryTimeOutNotFinishedException(_timeOut.Value,
                            _lastException is RecoveryTimeOutNotFinishedException rtonf ? rtonf.OriginalException : _lastException);
                        return false;
                    }
                    _timeOut = null;
                    _CurrentState = JDState.TimedOut;
                }

                do
                {
                    lastState = _CurrentState;
                    res = TryRepairStateAsync().Result;
                } while (!res && lastState != _CurrentState);

                return res;
            }
        }

        private DateTime _lastQueryDownloads;
        private SemaphoreSlim _queryDownloadsSemaphore = new SemaphoreSlim(1,1);
        public async Task<List<JD_FilePackage>> QueryDownloadStatesAsync()
        {
            if (DateTime.Now - _lastQueryDownloads < TimeSpan.FromMilliseconds(_settings.JD_MaxRefreshInterval))
            {
                return LastDownloadStates;
            }

            await _queryDownloadsSemaphore.WaitAsync();
            try
            {
                if (DateTime.Now - _lastQueryDownloads < TimeSpan.FromMilliseconds(_settings.JD_MaxRefreshInterval))
                {
                    return LastDownloadStates;
                }

                if (!IsReady())
                {
                    _logger.LogWarning(_lastException, $"Could not get ready. Current state: {_CurrentState}");
                    return new List<JD_FilePackage>();
                }
                var (state, list) = await Device_QueryDownloadPackagesAsync();
                _CurrentState = state;
                if (state != JDState.Ready)
                {
                    _logger.LogWarning(_lastException, $"Could not query download packages. State: {state}");
                    return new List<JD_FilePackage>();
                }
                //foreach (var p in pkgs.Item2) //.Where(pk => pk.PackageState == JD_PackageState.Finished))
                //{
                //    List<JD_ArchiveStatus> archiveStatus;
                //    (state, archiveStatus) = Device_GetArchiveInfoAsync(p).Result;
                //    if (archiveStatus.Any(s => s.ControllerStatus != JD_ControllerStatus.NA))
                //    {
                //        p.IsExtracting = true;
                //    }
                //}
                foreach (var p in list.Where(jp => jp.BytesLoaded >= jp.BytesTotal))
                {
                    List<JD_ArchiveStatus> archState;
                    (state, archState) = await Device_GetArchiveInfoAsync(p);
                    if (archState.Any(s => s.ControllerStatus != JD_ControllerStatus.NA))
                    {
                        p.IsExtracting = true;
                    }
                }

                LastDownloadStates.Clear();
                LastDownloadStates.AddRange(list);
                _lastQueryDownloads = DateTime.Now;

                return list;
            }
            finally
            {
                _queryDownloadsSemaphore.Release();
            }
        }

        public async Task<bool> RemoveDownloadPackageAsync(long uuid)
        {
            if (!IsReady())
            {
                _logger.LogWarning(_lastException, $"Could not get ready. Current state: {_CurrentState}");
                return false;
            }

            var (state, result) = await Device_RemoveDownloadPackagesAsync(uuid);

            if (state != JDState.Ready)
            {
                _logger.LogWarning(_lastException, $"Could not remove download packages. State: {state}");
            }

            return result;
        }

        public async Task<bool> RemoveDownloadPackageAsync(string downloadPath)
        {
            var package = LastDownloadStates.FirstOrDefault(pkg => Extensions.GetFileNamePlatformIndependent(pkg.SaveTo) == downloadPath);
            if (package != null)
            {
                return await RemoveDownloadPackageAsync(package.UUID);
            }
            _logger.LogInformation($"Could not resolve {downloadPath} to a UUID");
            return false;
        }

        private DateTime _lastQueryCrawledPackages;
        private SemaphoreSlim _crawledPackagesSemaphore = new SemaphoreSlim(1, 1);
        private List<JD_CrawledPackage> _cacheCrwaledPackages = new List<JD_CrawledPackage>();

        public async Task<List<JD_CrawledPackage>> QueryCrawledPackagesAsync()
        {
            if (DateTime.Now - _lastQueryCrawledPackages < TimeSpan.FromMilliseconds(_settings.JD_MaxRefreshInterval))
            {
                return _cacheCrwaledPackages;
            }

            await _crawledPackagesSemaphore.WaitAsync();
            try
            {
                if (DateTime.Now - _lastQueryCrawledPackages < TimeSpan.FromMilliseconds(_settings.JD_MaxRefreshInterval))
                {
                    return _cacheCrwaledPackages;
                }

                if (!IsReady())
                {
                    _logger.LogWarning(_lastException, $"Could not get ready. Current state: {_CurrentState}");
                    return new List<JD_CrawledPackage>();
                }
                var (state, list) = await Device_QueryCrawledPackagesAsync();
                if (state != JDState.Ready)
                {
                    _logger.LogWarning(_lastException, $"Could not remove download packages. State: {state}");
                    return new List<JD_CrawledPackage>();
                }

                _cacheCrwaledPackages.Clear();
                _cacheCrwaledPackages.AddRange(list);
                _lastQueryCrawledPackages = DateTime.Now;

                return list;
            } finally
            {
                _crawledPackagesSemaphore.Release();
            }
        }

        public async Task<bool> StartPackageDownloadAsync(List<long> uuids)
        {
            if (!IsReady())
            {
                _logger.LogWarning(_lastException, $"Could not get ready. Current state: {_CurrentState}");
                return false;
            }
            var state = await Device_MoveToDownloadsAsync(uuids);
            if (state != JDState.Ready)
            {
                _logger.LogWarning(_lastException, $"Could not start download of packages {uuids}. Current state: {_CurrentState}");
                return false;
            }
            return true;
        }

        public async Task<bool> AddDownloadLinksAsync(List<string> links, string packageName = null)
        {
            if (!IsReady())
            {
                _logger.LogWarning(_lastException, $"Could not get ready. Current state: {_CurrentState}");
                return false;
            }
            var (state, _) = await Device_AddLinksAsync(packageName, links);
            if (state != JDState.Ready)
            {
                _logger.LogWarning(_lastException, $"Could not add links to JDownloader. Current state: {_CurrentState}");
                return false;
            }
            return true;
        }

        public async Task<bool> RemoveQueriedDownloadLinksAsync(List<long> uuids)
        {
            if (!IsReady())
            {
                _logger.LogWarning(_lastException, $"Could not get ready. Current state: {_CurrentState}");
                return false;
            }
            var (state, res) = await Device_RemoveQueriedPackages(uuids);
            if (state != JDState.Ready)
            {
                _logger.LogWarning(_lastException, $"Could not remove links from JDownloader. Current state: {_CurrentState}");
            }
            return res;
        }
        #endregion


        // Here, error handling and state handling is provided
        #region error and state handling
        private async Task<bool> TryRepairStateAsync()
        {
            switch (_CurrentState)
            {
                case JDState.Ready:
                    return true;
                case JDState.NoConnection:
                case JDState.TimedOut:
                    return true;
                case JDState.Error:
                    if (_lastException is JDException jde)
                    {
                        _logger.LogInformation(_lastException, "Trying to recover from JD_Error");
                        if (jde.JDError.Type.Equals("TOKEN_INVALID", StringComparison.CurrentCultureIgnoreCase))
                        {
                            _CurrentState = await Server_LoginAsync();
                            _logger.LogInformation(jde, $"Recovering from jd_error resulted in {_CurrentState}");
                            return false;
                        }
                    }
                    _logger.LogWarning(_lastException, "Cannot recover from exception.");

                    _timeOut = DateTime.Now.AddMinutes(1);

                    return false;
                case JDState.NoDevice:
                    var (state, devices) = Server_ListDevicesAsync().Result;
                    if (state != JDState.Ready)
                    {
                        _CurrentState = state;
                        return false;
                    }
                    if (devices.Count == 0)
                    {
                        return false;
                    }
                    if (devices.Count > 1 && !string.IsNullOrEmpty(_settings.JD_PreferredClient))
                    {
                        var sel = devices.FirstOrDefault(d => d.Name.Contains(_settings.JD_PreferredClient, StringComparison.CurrentCultureIgnoreCase));
                        if (sel != null)
                        {
                            _selectedDeviceId = sel.Id;
                            _CurrentState = JDState.Ready;
                            return true;
                        }
                    }
                    _selectedDeviceId = devices[0].Id;
                    _CurrentState = JDState.Ready;
                    return true;
                case JDState.NotStarted:
                    _CurrentState = await Server_LoginAsync();
                    return false;
                case JDState.Overload:
                    _logger.LogWarning("Cannot recover from overload");
                    return false;
            }

            return false;
        }
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
            return $"{_apiBase}{query}{(hasOnlySigParam ? "?" : "&")}signature={sig}";
        }

        private T DecryptResponse<T>(string encryptedData, byte[] decKey, bool successStatusCode)
        {
            string responseString;
            try
            {
                responseString = Decrypt(Convert.FromBase64String(encryptedData), decKey);
            }
            catch (FormatException)
            {
                // response not in base64 format
                responseString = encryptedData;
            }

            //var opt = new JsonSerializerOptions();
            //opt.Converters.Add(new JsonConverterNullableJDPriority());
            if (!successStatusCode)
            {
                var err = JsonSerializer.Deserialize<JD_Error>(responseString);
                throw new JDException(err);
            }

            return JsonSerializer.Deserialize<T>(responseString);

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
            return DecryptResponse<T>(encBody, decKey, response.IsSuccessStatusCode);
        }
        #endregion


        // Here, API calls are implemented
        #region api calls
        private async Task<JDState> Server_LoginAsync()
        {
            if (_settings.JD_Use_Direct)
            {
                return JDState.Ready;
            }
            try
            {
                var loginSecret = CalcLoginSecret(_settings.JD_Email.ToLower() + _settings.JD_Password + "server");
                _deviceSecret = CalcLoginSecret(_settings.JD_Email.ToLower() + _settings.JD_Password + "device");
                var rid = RID;
                var query = $"/my/connect?email={_settings.JD_Email.ToLower()}&appkey={_appKey}&rid={rid}";
                var sig = CreateSignature(query, loginSecret);
                query = _apiBase + query + "&signature=" + sig;

                var response = await DecryptResponseAsync<JD_LoginResponse>(new HttpClient().PostAsync(query, null), loginSecret);
                if (response.Rid != rid)
                {
                    throw new InvalidDataException("The rid is not the expected one.");
                }
                _sessionToken = response.SessionToken;
                _regainToken = response.RegainToken;

                _deviceEncryptionToken = CalcSecret(_deviceSecret, _sessionToken);
                _serverEncryptionToken = CalcSecret(loginSecret, _sessionToken);

                return JDState.NoDevice;
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
            if (_settings.JD_Use_Direct)
            {
                return JDState.Ready;
            }
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
            if (_settings.JD_Use_Direct)
            {
                return (JDState.Ready, null);
            }
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
            string url;
            StringContent content;
            int rid = -1;
            if (_settings.JD_Use_Direct)
            {
                (url, content) = PrepareCallDeviceDirect(action, queryParams);
            }
            else
            {
                (url, content, rid) = PrepareCallDeviceRemote(action, queryParams);
            }

            try
            {
                _logger.LogDebug($"{DateTime.Now:R} Calling JD API");
                using (var httpClient = new HttpClient())
                {
                    var httpResponse = await httpClient.PostAsync(url, content);
                    var rawString = await httpResponse.Content.ReadAsStringAsync();
                    string responseString;
                    if (_settings.JD_Use_Direct)
                    {
                        responseString = rawString;
                    }
                    else
                    {
                        try
                        {
                            responseString = Decrypt(Convert.FromBase64String(rawString), _deviceEncryptionToken);
                        }
                        catch (FormatException)
                        {
                            // response not in base64 format
                            responseString = rawString;
                        }
                    }

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var err = JsonSerializer.Deserialize<JD_Error>(responseString);
                        throw new JDException(err);
                    }

                    var jdResponse = JsonSerializer.Deserialize<JD_Response<T>>(responseString);
                    if (!_settings.JD_Use_Direct && rid != jdResponse.Rid)
                    {
                        throw new InvalidDataException("The rid is not the expected one.");
                    }
                    return jdResponse.Data;
                }
            }
            catch (Exception hre)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Failed while executing action '{url}' with body '{content.ReadAsStringAsync().Result}'");
                Exception ex = hre;
                while (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                    ex = ex.InnerException;
                }
                _logger.LogError(sb.ToString());
                throw;
            }
        }

        private (string url, StringContent body) PrepareCallDeviceDirect(string action, params object[] queryParams)
        {
            var requestParams = "";
            if (queryParams != null && queryParams.Length != 0)
            {
                requestParams = queryParams.Select(p => JsonSerializer.Serialize(p, new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                })).Aggregate((s1, s2) => s1 + "&" + s2);
            }

            var url = _apiBase + action;
            if (!string.IsNullOrWhiteSpace(requestParams))
            {
                url += "?" + requestParams;
            }

            var body = new StringContent("");
            
            return (url, body);
        }
        private (string url, StringContent body, int rid) PrepareCallDeviceRemote(string action, params object[] queryParams)
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
            query = _apiBase + query; //CreateQuery(query, _deviceEncryptionToken, true);

            var body = JsonSerializer.Serialize(postData, new JsonSerializerOptions
            {
                IgnoreNullValues = true
            });
            var bodyEnc = Encrypt(body, _deviceEncryptionToken);
            var content = new StringContent(bodyEnc, Encoding.UTF8, "application/json");

            return (query, content, postData.Rid);

            //try
            //{
            //    var cnt = new StringContent(bodyEnc, Encoding.UTF8, "application/json");
            //    var wr = (HttpWebRequest)WebRequest.Create(query);
            //    wr.Method = "POST";
            //    using (var bodywriter = new StreamWriter(await wr.GetRequestStreamAsync()))
            //    {
            //        bodywriter.Write(bodyEnc);
            //    }
            //    _logger.LogInformation($"{DateTime.Now:R} Calling JD API");
            //    using (var webresponse = (HttpWebResponse)wr.GetResponse())
            //    {
            //        string webBody;
            //        using var bodyreader = new StreamReader(webresponse.GetResponseStream());
            //        webBody = bodyreader.ReadToEnd();

            //        var response = DecryptResponse<JD_Response<T>>(webBody, _deviceEncryptionToken, (int)webresponse.StatusCode >= 200 && (int)webresponse.StatusCode < 400);
            //        if (response.Rid != postData.Rid)
            //        {
            //            throw new InvalidDataException("The rid is not the expected one.");
            //        }
            //        return response.Data;
            //    }


            //    //var postTask = new HttpClient().PostAsync(query, cnt);
            //    //var response = await DecryptResponseAsync<JD_Response<T>>(postTask, _deviceEncryptionToken, returnUnmodified);
            //}
            //catch (Exception hre)
            //{
            //    var sb = new StringBuilder();
            //    sb.AppendLine($"Failed while executing action '{action}' with body '{body}'");
            //    Exception ex = hre;
            //    while (ex != null)
            //    {
            //        sb.AppendLine(ex.ToString());
            //        ex = ex.InnerException;
            //    }
            //    _logger.LogError(sb.ToString());
            //    throw hre;
            //}
        }

        private async Task<(JDState, List<JD_FilePackage>)> Device_QueryDownloadPackagesAsync()
        {
            try
            {
                var param = new JD_QueryDevices_Request_Params();

                var response = await CallDeviceAsync<List<JD_FilePackage>>("/downloadsV2/queryPackages", param);

                _logger.LogDebug(response.Count == 0 ? "No Packages returned" : response.Select(fp => $"{fp.Name}: state:'{fp.Status}'").Aggregate((s1, s2) => s1 + ", " + s2));

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
                // Todo: figure this out more precisely
                var response = await CallDeviceAsync<List<JD_ArchiveStatus>>("/extraction/getArchiveInfo", param, param);

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

        private async Task<JDState> Device_MoveToDownloadsAsync(List<long> uuids)
        {
            try
            {
                await CallDeviceAsync<dynamic>("/linkgrabberv2/moveToDownloadlist", new object[0], uuids.ToArray());
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

        private async Task<(JDState, bool)> Device_RemoveDownloadPackagesAsync(long uuid)
        {
            try
            {
                var resp = await CallDeviceAsync<string>("/downloadsV2/removeLinks", new long[] { }, new long[] { uuid });
                return (JDState.Ready, true);
            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, false);
            }
        }

        private async Task<(JDState, bool)> Device_RemoveQueriedPackages(List<long> uuids)
        {
            try
            {
                await CallDeviceAsync<dynamic>("/linkgrabberv2/removeLinks", new long[] { }, uuids.ToArray());
                return (JDState.Ready, true);
            } catch (Exception ex)
            {
                _lastException = ex;
                return (JDState.Error, false);
            }
        }
        #endregion
    }
}

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            Server_Login().Wait();
            Server_ListDevices().Wait();
            Server_Reconnect().Wait();
            Server_ListDevices().Wait();
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
            using var aes = new RijndaelManaged(); //Aes.Create();
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

        private string CreateQuery(string query, bool hasOnlySigParam = false)
        {
            var sig = CreateSignature(query, _serverEncryptionToken);
            return $"{_settings.JD_ApiPath}{query}{(hasOnlySigParam ? "?" : "&")}signature={sig}";
        }

        private async Task<T> DecryptResponseAsync<T>(Task<HttpResponseMessage> httpResponse, byte[] decKey)
        {
            var response = await httpResponse;
            var encBody = await response.Content.ReadAsStringAsync();
            var rawString = Decrypt(Convert.FromBase64String(encBody), decKey);
            return JsonSerializer.Deserialize<T>(rawString);
        }
        #endregion


        // Here, API calls are implemented
        #region api calls
        private async Task<JDState> Server_Login()
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
                query = CreateQuery(query);

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

        private async Task<(JDState, List<JD_Device>)> Server_ListDevices()
        {
            try
            {
                var rid = RID;
                var query = $"/my/listdevices?sessiontoken={_sessionToken}&rid={rid}";
                query = CreateQuery(query);
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

        #endregion
    }
}

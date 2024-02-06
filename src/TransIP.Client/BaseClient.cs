using TransIP.Client.Enums;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text.Json.Nodes;
using System;
using TransIP.Client.Models;

namespace TransIP.Client
{
    public class BaseClient
    {
        private readonly string _url;
        private readonly string _username;
        private readonly string _privateKey;
        private readonly ClientMode _mode;
        private readonly bool _onlyWhiteListedIps;
        private readonly string _labelPrefix;

        private readonly HttpClient _httpClient;
        private string _accessToken = string.Empty;
        private string _endpointUrl = "";

        private readonly JsonSerializerOptions _jsonOptions;

        public BaseClient(string url, string username, string privateKey, ClientMode clientMode, bool onlyWhiteListedIps, string labelPrefix)
        {
            _url = url;
            _username = username;
            _privateKey = privateKey;
            _mode = clientMode;
            _onlyWhiteListedIps = onlyWhiteListedIps;
            _labelPrefix = labelPrefix;

            try
            {
                _httpClient = new()
                {
                    BaseAddress = new Uri(_url),
                };
            }
            catch
            {
                throw; // Let the user handle the exception.
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
        }

        public void SetEndpoint (string endpointUrl)
        {
            _endpointUrl = endpointUrl;
        }

        private async Task _authenticateAsync ()
        {
            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                // Login and request accesstoken.
                await _requestAccessToken();
            }

            // Check if the accesstoken has expired?


            // Accesstoken provided and still valid, so don't change anything.
        }

        private async Task _requestAccessToken()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            Dictionary<string, string> requestBodyParameters = new()
            {
                { "login", _username },
                { "nonce", _createNonce(16) },
                { "read_only", (_mode == ClientMode.ReadOnly) ? "true" : "false" },
                { "expiration_time", "30 minutes" },
                { "label", _labelPrefix + ((int)t.TotalSeconds).ToString() },
                { "global_key", _onlyWhiteListedIps ? "false" : "true" }
            };

            string signature = _createSignature(JsonSerializer.Serialize(requestBodyParameters));

            // Do request the accesstoken
            try
            {
                HttpRequestMessage request = new(HttpMethod.Post, "auth");

                request.Headers.Add("Signature", signature);

                request.Content = new StringContent(JsonSerializer.Serialize(requestBodyParameters), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var token = await response.Content.ReadFromJsonAsync<Authentication>(_jsonOptions);

                    if (token != null && token.Token != null)
                    {
                        _accessToken = token.Token;
                    }
                    else
                    {
                        throw new Exception($"Did not receive the expected token json object");
                    }
                }
                else
                {
                    throw new Exception($"Did not receive the accesstoken, received HTTP Status code: {response.StatusCode}");
                }
            }
            catch
            {
                throw;
            }
        }

        private string _createNonce (int length)
        {
            Random random = new Random();
            var bytes = new Byte[length];
            random.NextBytes(bytes);

            var hexArray = Array.ConvertAll(bytes, x => x.ToString("X2"));
            var hexStr = String.Concat(hexArray);

            return hexStr.ToLower();
        }

        private string _createSignature (string strToEncrypt)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(strToEncrypt);
            byte[] inputHash = SHA512.Create().ComputeHash(inputBytes);

            string strippedKey = Regex.Replace(Regex.Replace(_privateKey, @"-----BEGIN (?:RSA )?PRIVATE KEY-----", string.Empty), @"-----END (?:RSA )?PRIVATE KEY-----", string.Empty)
                                      .Replace("\r", string.Empty).Replace("\n", string.Empty);
            byte[] privateKey = Convert.FromBase64String(strippedKey);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportPkcs8PrivateKey(privateKey, out _);

                byte[] output = rsa.SignHash(inputHash, "SHA512");

                return Convert.ToBase64String(output);
            }
        }
        
        public async Task<T?> PostAsync<T> (string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            var response = await _sendAsync(HttpMethod.Post, urlAddition, data, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();

                // Convert.
                return JsonSerializer.Deserialize<T>(responseStr, _jsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        public async Task<T?> PutAsync<T>(string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            var response = await _sendAsync(HttpMethod.Put, urlAddition, data, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();

                // Convert.
                return JsonSerializer.Deserialize<T>(responseStr, _jsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        public async Task<T?> PatchAsync<T>(string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            var response = await _sendAsync(HttpMethod.Patch, urlAddition, data, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();

                // Convert.
                return JsonSerializer.Deserialize<T>(responseStr, _jsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        public async Task<T?> DeleteAsync<T>(string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            var response = await _sendAsync(HttpMethod.Delete, urlAddition, data, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();

                // Convert.
                return JsonSerializer.Deserialize<T>(responseStr, _jsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        public async Task<T?> GetAsync<T>(string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            var response = await _sendAsync(HttpMethod.Get, urlAddition, data, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<T>(jsonResult, _jsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        private async Task<HttpResponseMessage> _sendAsync (HttpMethod method, string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            HttpRequestMessage request = new (method, _endpointUrl + urlAddition);

            request.Method = method;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            
            request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"); ;

            if (cancellationToken != null)
            {
                return await _httpClient.SendAsync(request, (CancellationToken)cancellationToken);
            }

            return await _httpClient.SendAsync(request);
        }
    }
}

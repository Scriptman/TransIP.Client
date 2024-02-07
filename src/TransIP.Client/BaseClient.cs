using TransIP.Client.Enums;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TransIP.Client.Models;
using System.Net;
using TransIP.Client.DataTransferObjects;
using System.Linq.Expressions;
using TransIP.Client.Exceptions;
using TransIP.Client.JsonConverters;

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

        public JsonSerializerOptions JsonOptions;

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

            JsonOptions = new JsonSerializerOptions
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
                    var token = await response.Content.ReadFromJsonAsync<Authentication>(JsonOptions);

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
                    try
                    {
                        var errorDto = await response.Content.ReadFromJsonAsync<ErrorDto>();
                        throw new Exception(errorDto?.Error ?? "Some error occured while requesting the access token, HTTP StatusCode: " + response.StatusCode);
                    }
                    catch
                    {
                        throw;
                    }
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
                return JsonSerializer.Deserialize<T>(responseStr, JsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        public async Task<HttpResponseMessage> PutAsync(string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            return await _sendAsync(HttpMethod.Put, urlAddition, data, cancellationToken);
        }

        public async Task<T?> PatchAsync<T>(string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we neet to create or refresh the accesstoken?

            var response = await _sendAsync(HttpMethod.Patch, urlAddition, data, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();

                // Convert.
                return JsonSerializer.Deserialize<T>(responseStr, JsonOptions);
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
                return JsonSerializer.Deserialize<T>(responseStr, JsonOptions);
            }
            else
            {
                throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string additionalUrl = "", Dictionary<string,string>? urlParameters = null, object? data = null, CancellationToken? cancellationToken = null)
        {
            await _authenticateAsync(); // Do we need to create or refresh the accesstoken?

            string parsedUrlParameters = string.Empty;

            if (urlParameters != null && urlParameters.Any())
            {
                var strKeyValuePairs = urlParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value));
                var strUrlParameters = string.Join("&", strKeyValuePairs);
                parsedUrlParameters = "?" + strUrlParameters;
            }

            return await _sendAsync(HttpMethod.Get, additionalUrl + parsedUrlParameters, data, cancellationToken);
        }

        private async Task<HttpResponseMessage> _sendAsync (HttpMethod method, string urlAddition, object? data, CancellationToken? cancellationToken = null)
        {
            // Correct urlAddition with starting slash
            if (!urlAddition.StartsWith("/"))
            {
                urlAddition = "/" + urlAddition;
            }
             
            HttpRequestMessage request = new (method, _endpointUrl + urlAddition);

            request.Method = method;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var jsonSendOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LowerCaseNamingPolicy()
            };

            request.Content = new StringContent(JsonSerializer.Serialize(data, jsonSendOptions), Encoding.UTF8, "application/json");

            HttpResponseMessage response = new(); 

            if (cancellationToken != null)
            {
                response =  await _httpClient.SendAsync(request, (CancellationToken)cancellationToken);
            }

            response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            // Throw exception, let the user handle this.
            try
            {
                var errorDto = await response.Content.ReadFromJsonAsync<ErrorDto>();

                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        throw new TransIpNotFoundException(errorDto?.Error ?? "No error message received from TransIP, but the HTTP StatusCode was 404 - Not found");
                    case HttpStatusCode.NotAcceptable:
                        throw new TransIpInvalidDomainException(errorDto?.Error ?? "No error message received from TransIP, but the HTTP StatusCode was 406 - Not Acceptable");
                    case HttpStatusCode.Forbidden:
                        if (_mode == ClientMode.ReadOnly)
                        {
                            return response;
                        }

                        throw new Exception(errorDto?.Error ?? $"No error message received from TransIP, but the HTTP StatusCode was {response.StatusCode}");
                    default:
                        throw new Exception(errorDto?.Error ?? $"No error message received from TransIP, but the HTTP StatusCode was {response.StatusCode}");
                }
            }
            catch
            {
                throw;
            }
        }
    }
}

using TransIP.Client.Enums;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Net;
using TransIP.Client.DataTransferObjects;
using TransIP.Client.Exceptions;
using TransIP.Client.JsonConverters;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TransIP.Client.Helpers;

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
                _httpClient = new HttpClient()
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

            Dictionary<string, string> requestBodyParameters = new Dictionary<string, string>()
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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "auth");

                request.Headers.Add("Signature", signature);

                request.Content = new StringContent(JsonSerializer.Serialize(requestBodyParameters), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResult = await response.Content.ReadAsStreamAsync();
                    var auth = await JsonSerializer.DeserializeAsync<TokenDto>(jsonResult, JsonOptions);

                    if (auth != null && auth.Token != null)
                    {
                        _accessToken = auth.Token;
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
                        var jsonResult = await response.Content.ReadAsStreamAsync();
                        var errorDto = await JsonSerializer.DeserializeAsync<ErrorDto>(jsonResult, JsonOptions);
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
            //return EncryptionHelper.Encode(EncryptionHelper.Sign(_privateKey,strToEncrypt));

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
        
        public async Task<HttpResponseMessage> PostAsync(string additionalUrl, object data)
        {
            var request = await _createRequest(HttpMethod.Post, additionalUrl);
            request.Content = _transformDtoToJson(data);

            return await _sendRequest(request);
        }

        public async Task<HttpResponseMessage> PutAsync(string additionalUrl, object data)
        {
            var request = await _createRequest(HttpMethod.Put, additionalUrl);
            request.Content = _transformDtoToJson(data);

            return await _sendRequest(request);
        }

        public async Task<HttpResponseMessage> PatchAsync(string additionalUrl, object data)
        {
            var request = await _createRequest(new HttpMethod("PATCH"), additionalUrl);
            request.Content = _transformDtoToJson(data);

            return await _sendRequest(request);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string additionalUrl)
        {
            var request = await _createRequest(HttpMethod.Delete, additionalUrl);
            //request.Content = _transformDtoToJson(data);

            return await _sendRequest(request);
        }

        public async Task<HttpResponseMessage> GetAsync(string additionalUrl = "", Dictionary<string,string>? urlParameters = null)
        {
            var request = await _createRequest(HttpMethod.Get, additionalUrl, urlParameters);
            //request.Content = _transformDtoToJson(request);

            return await _sendRequest(request);
        }

        private async Task<HttpResponseMessage> _sendRequest(HttpRequestMessage request)
        {
            var result = await _httpClient.SendAsync(request);

            if (!result.IsSuccessStatusCode)
            {
                return await _handleRequestFailure(result); // Can return content of 401 + using mode ReadOnly
            }

            return result;
        }

        private async Task<HttpRequestMessage> _createRequest(HttpMethod method, string relativeUrl, Dictionary<string, string>? queryParameters = null)
        {
            await _authenticateAsync(); // Do we need to create or refresh the accesstoken?

            // Correct urlAddition with starting slash
            if (!relativeUrl.StartsWith("/"))
            {
                relativeUrl = "/" + relativeUrl;
            }

            string parsedUrlParameters = string.Empty;

            if (queryParameters != null && queryParameters.Any())
            {
                var strKeyValuePairs = queryParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value));
                var strUrlParameters = string.Join("&", strKeyValuePairs);
                parsedUrlParameters = "?" + strUrlParameters;
            }

            HttpRequestMessage request = new HttpRequestMessage(method, _endpointUrl + relativeUrl + parsedUrlParameters);

            request.Method = method;

            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            return request;
        }

        private StringContent _transformDtoToJson ( object dtoObject )
        {
            var jsonSendOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LowerCaseNamingPolicy()
            };

            return new StringContent(JsonSerializer.Serialize(dtoObject, jsonSendOptions), Encoding.UTF8, "application/json");
        }

        private async Task<HttpResponseMessage> _handleRequestFailure(HttpResponseMessage response)
        {
            try
            {
                var resultJson = await response.Content.ReadAsStreamAsync();
                var errorDto = await JsonSerializer.DeserializeAsync<ErrorDto>(resultJson);

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
            finally
            {
                response.Dispose();
            }
        }
    }
}

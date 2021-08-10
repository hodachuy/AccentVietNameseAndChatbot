using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Common.ApiService
{
    public class BaseApiService : IDisposable
    {
        private readonly TimeSpan _timeout;
        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private const string ClientUserAgent = "api-v1";
        private const string MediaTypeJson = "application/json";

        public BaseApiService(string baseUrl, string apiKey, TimeSpan? timeout = null)
        {
            _baseUrl = NormalizeBaseUrl(baseUrl);
            _apiKey = apiKey;
            _timeout = timeout ?? TimeSpan.FromSeconds(30);
        }

        public async Task<string> PostAsync(string url, object input)
        {
            EnsureHttpClientCreated();

            using (var requestContent = new StringContent(ConvertToJsonString(input), Encoding.UTF8, MediaTypeJson))
            {
                using (var response = await _httpClient.PostAsync(url, requestContent).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public async Task<TResult> PostAsync<TResult>(string url, object input) where TResult : class, new()
        {
            var strResponse = await PostAsync(url, input);

            return JsonConvert.DeserializeObject<TResult>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        public async Task<TResult> GetAsync<TResult>(string url) where TResult : class, new()
        {
            var strResponse = await GetAsync(url);

            return JsonConvert.DeserializeObject<TResult>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        public async Task<string> GetAsync(string url)
        {
            EnsureHttpClientCreated();
            using (var response = await _httpClient.GetAsync(url).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> PutAsync(string url, object input)
        {
            return await PutAsync(url, new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, MediaTypeJson));
        }

        public async Task<string> PutAsync(string url, HttpContent content)
        {
            EnsureHttpClientCreated();

            using (var response = await _httpClient.PutAsync(url, content).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> DeleteAsync(string url)
        {
            EnsureHttpClientCreated();

            using (var response = await _httpClient.DeleteAsync(url).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public void Dispose()
        {
            _httpClientHandler?.Dispose();
            _httpClient?.Dispose();
        }

        private void CreateHttpClient()
        {
            _httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            //_httpClient = new HttpClient(_httpClientHandler, false)
            //{
            //    Timeout = _timeout
            //};

            _httpClient = new HttpClient(_httpClientHandler, false);

            _httpClient.DefaultRequestHeaders.Accept.Clear();

            //_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ClientUserAgent);

            if (!String.IsNullOrWhiteSpace(_baseUrl))
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }

            if (!String.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeJson));
        }

        private void EnsureHttpClientCreated()
        {
            if (_httpClient == null)
            {
                CreateHttpClient();
            }
        }

        private static string ConvertToJsonString(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        private static string NormalizeBaseUrl(string url)
        {
            return url.EndsWith("/") ? url : url + "/";
        }
    }
}

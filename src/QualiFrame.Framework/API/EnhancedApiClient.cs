using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;
using RestSharp;

namespace QualiFrame.Framework.API
{
    /// <summary>
    /// Enhanced API client with logging, retry, circuit breaker, and schema validation.
    /// </summary>
    public class EnhancedApiClient
    {
        private readonly RestClient _client;
        private readonly TestConfiguration _config;
        private readonly ApiPerformanceMonitor _performanceMonitor;
        private readonly CircuitBreaker? _circuitBreaker;
        private readonly bool _enableCircuitBreaker;

        public EnhancedApiClient(string? baseUrl = null, bool enableCircuitBreaker = false)
        {
            _config = TestConfiguration.Instance;
            var url = baseUrl ?? _config.ApiBaseUrl;
            
            var options = new RestClientOptions(url)
            {
                Timeout = TimeSpan.FromSeconds(_config.ApiTimeoutSeconds)
            };

            _client = new RestClient(options);
            _performanceMonitor = new ApiPerformanceMonitor();
            _enableCircuitBreaker = enableCircuitBreaker;

            if (_enableCircuitBreaker)
            {
                _circuitBreaker = new CircuitBreaker();
            }

            Logger.Information("Enhanced API client initialized with base URL: {BaseUrl}", url);
        }

        public void SetDefaultHeaders(string name, string value)
        {
            _client.AddDefaultHeader(name, value);
            Logger.Debug("Default header set: {Name} = {Value}", name, "***");
        }

        public void SetAuthenticationToken(string token)
        {
            _client.AddDefaultHeader("Authorization", $"Bearer {token}");
            Logger.Debug("Authentication token set");
        }

        public void SetBasicAuth(string username, string password)
        {
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
            _client.AddDefaultHeader("Authorization", $"Basic {credentials}");
            Logger.Debug("Basic authentication set for user: {Username}", username);
        }

        public async Task<RestResponse> GetAsync(string endpoint, Dictionary<string, string>? queryParams = null)
        {
            return await ExecuteRequestAsync(Method.Get, endpoint, null, queryParams);
        }

        public async Task<RestResponse> PostAsync(string endpoint, object? body = null, Dictionary<string, string>? queryParams = null)
        {
            return await ExecuteRequestAsync(Method.Post, endpoint, body, queryParams);
        }

        public async Task<RestResponse> PutAsync(string endpoint, object? body = null, Dictionary<string, string>? queryParams = null)
        {
            return await ExecuteRequestAsync(Method.Put, endpoint, body, queryParams);
        }

        public async Task<RestResponse> DeleteAsync(string endpoint, Dictionary<string, string>? queryParams = null)
        {
            return await ExecuteRequestAsync(Method.Delete, endpoint, null, queryParams);
        }

        public async Task<RestResponse> PatchAsync(string endpoint, object? body = null, Dictionary<string, string>? queryParams = null)
        {
            return await ExecuteRequestAsync(Method.Patch, endpoint, body, queryParams);
        }

        public bool ValidateResponse(RestResponse response, JObject schema, out List<string> errors)
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                errors = new List<string> { "Response content is empty" };
                return false;
            }

            return SchemaValidator.Validate(response.Content, schema, out errors);
        }

        public bool ValidateRequiredFields(RestResponse response, params string[] requiredFields)
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                Logger.Warning("Response content is empty, cannot validate required fields");
                return false;
            }

            return SchemaValidator.ValidateRequiredFields(response.Content, requiredFields);
        }

        public ApiPerformanceStats GetPerformanceStats(string? endpoint = null)
        {
            if (endpoint == null)
            {
                return _performanceMonitor.GetOverallStats();
            }
            else
            {
                return _performanceMonitor.GetStats(endpoint);
            }
        }

        private async Task<RestResponse> ExecuteRequestAsync(
            Method method, 
            string endpoint, 
            object? body = null, 
            Dictionary<string, string>? queryParams = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = new RestRequest(endpoint, method);

            if (queryParams != null)
            {
                foreach (var param in queryParams)
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }
            }

            if (body != null)
            {
                request.AddJsonBody(body);
                Logger.Debug("Request body: {Body}", JsonConvert.SerializeObject(body));
            }

            Logger.Information("Sending {Method} request to {Endpoint}", method, endpoint);

            RestResponse response;

            try
            {
                var retryPolicy = RetryPolicy.CreateHttpRetryPolicy();

                if (_enableCircuitBreaker && _circuitBreaker != null)
                {
                    response = await _circuitBreaker.ExecuteAsync(async () =>
                    {
                        var resp = await retryPolicy.ExecuteAsync(async () => await _client.ExecuteAsync(request));
                        return resp;
                    });
                }
                else
                {
                    response = await retryPolicy.ExecuteAsync(async () => await _client.ExecuteAsync(request));
                }

                stopwatch.Stop();

                var statusCode = (int)response.StatusCode;
                _performanceMonitor.RecordCall(
                    endpoint,
                    method.ToString(),
                    stopwatch.Elapsed,
                    statusCode,
                    response.IsSuccessStatusCode);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Information(
                        "Request successful: {Method} {Endpoint}, Status: {StatusCode}, Duration: {Duration}ms",
                        method, endpoint, response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
                }
                else
                {
                    var contentPreview = response.Content != null && response.Content.Length > 200 
                        ? response.Content.Substring(0, 200) + "..." 
                        : response.Content ?? "";
                    Logger.Warning(
                        "Request failed: {Method} {Endpoint}, Status: {StatusCode}, Duration: {Duration}ms, Response: {Response}",
                        method, endpoint, response.StatusCode, stopwatch.Elapsed.TotalMilliseconds, contentPreview);
                }

                if (!string.IsNullOrEmpty(response.Content))
                {
                    var contentPreview = response.Content.Length > 500 
                        ? response.Content.Substring(0, 500) + "..." 
                        : response.Content;
                    Logger.Debug("Response content: {Content}", contentPreview);
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _performanceMonitor.RecordCall(
                    endpoint,
                    method.ToString(),
                    stopwatch.Elapsed,
                    null,
                    false);

                Logger.Error(ex, "Request exception: {Method} {Endpoint}, Duration: {Duration}ms",
                    method, endpoint, stopwatch.Elapsed.TotalMilliseconds);

                throw new APIException(
                    $"API request failed: {ex.Message}",
                    ex,
                    endpoint: endpoint,
                    testId: Logger.TestId);
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            Logger.Information("API client disposed");
        }
    }
}

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API.GraphQL
{
    /// <summary>
    /// GraphQL client for testing GraphQL APIs.
    /// </summary>
    public class GraphQLClient
    {
        private readonly GraphQLHttpClient _client;
        private readonly TestConfiguration _config;
        private readonly ApiPerformanceMonitor _performanceMonitor;

        public GraphQLClient(string? endpoint = null)
        {
            _config = TestConfiguration.Instance;
            var url = endpoint ?? _config.GraphQLEndpoint ?? "https://api.example.com/graphql";

            var options = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(url)
            };

            _client = new GraphQLHttpClient(options, new NewtonsoftJsonSerializer());
            _client.HttpClient.Timeout = TimeSpan.FromSeconds(_config.ApiTimeoutSeconds);
            _performanceMonitor = new ApiPerformanceMonitor();

            Logger.Information("GraphQL client initialized with endpoint: {Endpoint}", url);
        }

        /// <summary>
        /// Sets authentication token.
        /// </summary>
        public void SetAuthenticationToken(string token)
        {
            _client.HttpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            Logger.Debug("GraphQL authentication token set");
        }

        /// <summary>
        /// Sets custom header.
        /// </summary>
        public void SetHeader(string name, string value)
        {
            _client.HttpClient.DefaultRequestHeaders.Add(name, value);
            Logger.Debug("GraphQL header set: {Name}", name);
        }

        /// <summary>
        /// Executes a GraphQL query.
        /// </summary>
        public async Task<GraphQLResponse<JObject>> QueryAsync(string query, object? variables = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var request = new GraphQLRequest
            {
                Query = query,
                Variables = variables != null ? JObject.FromObject(variables) : null
            };

            Logger.Information("Executing GraphQL query");
            Logger.Debug("Query: {Query}", query);
            if (variables != null)
            {
                Logger.Debug("Variables: {Variables}", JsonConvert.SerializeObject(variables));
            }

            try
            {
                var response = await _client.SendQueryAsync<JObject>(request);
                stopwatch.Stop();

                _performanceMonitor.RecordCall(
                    "GraphQL Query",
                    "QUERY",
                    stopwatch.Elapsed,
                    200,
                    response.Errors == null || response.Errors.Length == 0);

                if (response.Errors != null && response.Errors.Length > 0)
                {
                    Logger.Warning("GraphQL query returned errors: {Errors}", 
                        JsonConvert.SerializeObject(response.Errors));
                }
                else
                {
                    Logger.Information("GraphQL query executed successfully in {Duration}ms", 
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordCall("GraphQL Query", "QUERY", stopwatch.Elapsed, null, false);
                
                Logger.Error(ex, "GraphQL query failed: {Error}", ex.Message);
                throw new APIException(
                    $"GraphQL query failed: {ex.Message}",
                    ex,
                    endpoint: "GraphQL Query",
                    testId: Logger.TestId);
            }
        }

        /// <summary>
        /// Executes a GraphQL mutation.
        /// </summary>
        public async Task<GraphQLResponse<JObject>> MutateAsync(string mutation, object? variables = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var request = new GraphQLRequest
            {
                Query = mutation,
                Variables = variables != null ? JObject.FromObject(variables) : null
            };

            Logger.Information("Executing GraphQL mutation");
            Logger.Debug("Mutation: {Mutation}", mutation);
            if (variables != null)
            {
                Logger.Debug("Variables: {Variables}", JsonConvert.SerializeObject(variables));
            }

            try
            {
                var response = await _client.SendMutationAsync<JObject>(request);
                stopwatch.Stop();

                _performanceMonitor.RecordCall(
                    "GraphQL Mutation",
                    "MUTATION",
                    stopwatch.Elapsed,
                    200,
                    response.Errors == null || response.Errors.Length == 0);

                if (response.Errors != null && response.Errors.Length > 0)
                {
                    Logger.Warning("GraphQL mutation returned errors: {Errors}", 
                        JsonConvert.SerializeObject(response.Errors));
                }
                else
                {
                    Logger.Information("GraphQL mutation executed successfully in {Duration}ms", 
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordCall("GraphQL Mutation", "MUTATION", stopwatch.Elapsed, null, false);
                
                Logger.Error(ex, "GraphQL mutation failed: {Error}", ex.Message);
                throw new APIException(
                    $"GraphQL mutation failed: {ex.Message}",
                    ex,
                    endpoint: "GraphQL Mutation",
                    testId: Logger.TestId);
            }
        }

        /// <summary>
        /// Validates GraphQL response structure.
        /// </summary>
        public bool ValidateResponse(GraphQLResponse<JObject> response, params string[] requiredFields)
        {
            if (response.Data == null)
            {
                Logger.Warning("GraphQL response data is null");
                return false;
            }

            foreach (var field in requiredFields)
            {
                if (response.Data[field] == null)
                {
                    Logger.Warning("Required field '{Field}' not found in GraphQL response", field);
                    return false;
                }
            }

            Logger.Debug("GraphQL response validation passed");
            return true;
        }

        /// <summary>
        /// Gets performance statistics.
        /// </summary>
        public ApiPerformanceStats GetPerformanceStats()
        {
            return _performanceMonitor.GetOverallStats();
        }

        /// <summary>
        /// Disposes the client.
        /// </summary>
        public void Dispose()
        {
            _client?.Dispose();
            Logger.Information("GraphQL client disposed");
        }
    }
}

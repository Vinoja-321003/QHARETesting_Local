using System;
using System.Threading.Tasks;
using RestSharp;
using Polly;
using Polly.Retry;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API
{
    /// <summary>
    /// Retry policy configuration for API calls.
    /// </summary>
    public static class RetryPolicy
    {
        private static readonly TestConfiguration _config = TestConfiguration.Instance;

        /// <summary>
        /// Creates a retry policy for HTTP requests.
        /// </summary>
        public static AsyncRetryPolicy<RestResponse> CreateHttpRetryPolicy()
        {
            return Policy
                .HandleResult<RestResponse>(r => !r.IsSuccessStatusCode)
                .Or<Exception>()
                .WaitAndRetryAsync(
                    retryCount: _config.ApiRetryCount,
                    sleepDurationProvider: retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        if (outcome.Exception != null)
                        {
                            Logger.Warning(
                                "API request retry {RetryCount}/{MaxRetries} after {Delay}s. Exception: {Exception}",
                                retryCount, _config.ApiRetryCount, timespan.TotalSeconds, outcome.Exception.Message);
                        }
                        else if (outcome.Result != null)
                        {
                            Logger.Warning(
                                "API request retry {RetryCount}/{MaxRetries} after {Delay}s. Status: {StatusCode}",
                                retryCount, _config.ApiRetryCount, timespan.TotalSeconds, outcome.Result.StatusCode);
                        }
                    });
        }

        /// <summary>
        /// Creates a retry policy for generic operations.
        /// </summary>
        public static Polly.Retry.RetryPolicy CreateGenericRetryPolicy(int? retryCount = null)
        {
            var maxRetries = retryCount ?? _config.ApiRetryCount;
            
            return Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: maxRetries,
                    sleepDurationProvider: retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        Logger.Warning(
                            "Operation retry {RetryCount}/{MaxRetries} after {Delay}s. Exception: {Exception}",
                            retryCount, maxRetries, timespan.TotalSeconds, exception.Message);
                    });
        }
    }
}

using System;
using System.Threading.Tasks;
using RestSharp;
using Polly;
using Polly.CircuitBreaker;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API
{
    /// <summary>
    /// Circuit breaker pattern implementation for API calls.
    /// </summary>
    public class CircuitBreaker
    {
        private readonly AsyncCircuitBreakerPolicy<RestResponse> _circuitBreaker;
        private readonly int _failureThreshold;
        private readonly TimeSpan _durationOfBreak;

        public CircuitBreaker(int failureThreshold = 5, int durationOfBreakSeconds = 30)
        {
            _failureThreshold = failureThreshold;
            _durationOfBreak = TimeSpan.FromSeconds(durationOfBreakSeconds);

            _circuitBreaker = Policy
                .HandleResult<RestResponse>(r => !r.IsSuccessStatusCode)
                .Or<Exception>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: failureThreshold,
                    durationOfBreak: _durationOfBreak,
                    onBreak: (result, duration) =>
                    {
                        Logger.Warning(
                            "Circuit breaker opened. Failures: {Failures}, Duration: {Duration}s",
                            failureThreshold, duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        Logger.Information("Circuit breaker reset");
                    },
                    onHalfOpen: () =>
                    {
                        Logger.Information("Circuit breaker half-open - testing connection");
                    });
        }

        /// <summary>
        /// Executes an action through the circuit breaker.
        /// </summary>
        public async Task<RestResponse> ExecuteAsync(
            Func<Task<RestResponse>> action)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(action);
            }
            catch (BrokenCircuitException ex)
            {
                Logger.Error(ex, "Circuit breaker is open. Request rejected.");
                throw;
            }
        }

        /// <summary>
        /// Gets the circuit breaker state.
        /// </summary>
        public CircuitState State => _circuitBreaker.CircuitState;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API
{
    /// <summary>
    /// Monitors API performance metrics.
    /// </summary>
    public class ApiPerformanceMonitor
    {
        private readonly List<ApiCallMetrics> _metrics = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Records an API call with performance metrics.
        /// </summary>
        public void RecordCall(string endpoint, string method, TimeSpan duration, int? statusCode = null, bool success = true)
        {
            lock (_lock)
            {
                _metrics.Add(new ApiCallMetrics
                {
                    Endpoint = endpoint,
                    Method = method,
                    Duration = duration,
                    StatusCode = statusCode,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                });

                Logger.Debug("API call recorded: {Method} {Endpoint}, Duration: {Duration}ms, Status: {StatusCode}",
                    method, endpoint, duration.TotalMilliseconds, statusCode ?? 0);
            }
        }

        /// <summary>
        /// Gets performance statistics for an endpoint.
        /// </summary>
        public ApiPerformanceStats GetStats(string endpoint)
        {
            lock (_lock)
            {
                var endpointMetrics = _metrics.Where(m => m.Endpoint == endpoint).ToList();
                
                if (endpointMetrics.Count == 0)
                {
                    return new ApiPerformanceStats { Endpoint = endpoint };
                }

                var durations = endpointMetrics.Select(m => m.Duration.TotalMilliseconds).ToList();
                
                return new ApiPerformanceStats
                {
                    Endpoint = endpoint,
                    TotalCalls = endpointMetrics.Count,
                    SuccessCount = endpointMetrics.Count(m => m.Success),
                    FailureCount = endpointMetrics.Count(m => !m.Success),
                    AverageDuration = TimeSpan.FromMilliseconds(durations.Average()),
                    MinDuration = TimeSpan.FromMilliseconds(durations.Min()),
                    MaxDuration = TimeSpan.FromMilliseconds(durations.Max()),
                    P95Duration = TimeSpan.FromMilliseconds(CalculatePercentile(durations, 95)),
                    P99Duration = TimeSpan.FromMilliseconds(CalculatePercentile(durations, 99))
                };
            }
        }

        /// <summary>
        /// Gets overall performance statistics.
        /// </summary>
        public ApiPerformanceStats GetOverallStats()
        {
            lock (_lock)
            {
                if (_metrics.Count == 0)
                {
                    return new ApiPerformanceStats();
                }

                var durations = _metrics.Select(m => m.Duration.TotalMilliseconds).ToList();

                return new ApiPerformanceStats
                {
                    Endpoint = "All Endpoints",
                    TotalCalls = _metrics.Count,
                    SuccessCount = _metrics.Count(m => m.Success),
                    FailureCount = _metrics.Count(m => !m.Success),
                    AverageDuration = TimeSpan.FromMilliseconds(durations.Average()),
                    MinDuration = TimeSpan.FromMilliseconds(durations.Min()),
                    MaxDuration = TimeSpan.FromMilliseconds(durations.Max()),
                    P95Duration = TimeSpan.FromMilliseconds(CalculatePercentile(durations, 95)),
                    P99Duration = TimeSpan.FromMilliseconds(CalculatePercentile(durations, 99))
                };
            }
        }

        /// <summary>
        /// Clears all metrics.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _metrics.Clear();
                Logger.Information("API performance metrics cleared");
            }
        }

        private double CalculatePercentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
                return 0;

            values.Sort();
            var index = (int)Math.Ceiling(percentile / 100.0 * values.Count) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count - 1))];
        }

        private class ApiCallMetrics
        {
            public string Endpoint { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public TimeSpan Duration { get; set; }
            public int? StatusCode { get; set; }
            public bool Success { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    /// <summary>
    /// API performance statistics.
    /// </summary>
    public class ApiPerformanceStats
    {
        public string Endpoint { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan P95Duration { get; set; }
        public TimeSpan P99Duration { get; set; }

        public double SuccessRate => TotalCalls > 0 ? (double)SuccessCount / TotalCalls * 100 : 0;
    }
}

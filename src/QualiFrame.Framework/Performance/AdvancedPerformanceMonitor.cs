using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using QualiFrame.Framework.API;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Performance
{
    /// <summary>
    /// Comprehensive performance monitoring combining WebDriver, API, and resource metrics.
    /// </summary>
    public class AdvancedPerformanceMonitor : IDisposable
    {
        private readonly WebDriverPerformanceMonitor _webDriverMonitor;
        private readonly ResourceUtilizationTracker _resourceTracker;
        private readonly ApiPerformanceMonitor _apiMonitor;
        private readonly PerformanceOptimizer _optimizer;
        private bool _disposed = false;

        public AdvancedPerformanceMonitor()
        {
            _webDriverMonitor = new WebDriverPerformanceMonitor();
            _resourceTracker = new ResourceUtilizationTracker();
            _apiMonitor = new ApiPerformanceMonitor();
            _optimizer = new PerformanceOptimizer();

            Logger.Information("Advanced performance monitor initialized");
        }

        /// <summary>
        /// Records page load metrics.
        /// </summary>
        public PageLoadMetrics RecordPageLoad(IWebDriver driver, string? pageName = null)
        {
            _resourceTracker.TakeSnapshot($"PageLoad_{pageName ?? "Unknown"}");
            return _webDriverMonitor.RecordPageLoad(driver, pageName);
        }

        /// <summary>
        /// Records API call metrics.
        /// </summary>
        public void RecordApiCall(string endpoint, string method, TimeSpan duration, int? statusCode = null, bool success = true)
        {
            _apiMonitor.RecordCall(endpoint, method, duration, statusCode, success);
        }

        /// <summary>
        /// Gets API performance monitor instance.
        /// </summary>
        public ApiPerformanceMonitor GetApiMonitor() => _apiMonitor;

        /// <summary>
        /// Takes a resource utilization snapshot.
        /// </summary>
        public ResourceSnapshot TakeResourceSnapshot(string? label = null)
        {
            return _resourceTracker.TakeSnapshot(label);
        }

        /// <summary>
        /// Gets comprehensive performance report.
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            var webDriverStats = _webDriverMonitor.GetStatistics();
            var resourceStats = _resourceTracker.GetStatistics();
            var apiStats = _apiMonitor.GetOverallStats();
            var summary = _optimizer.GetSummary(webDriverStats, resourceStats, apiStats);

            return new PerformanceReport
            {
                Timestamp = DateTime.UtcNow,
                WebDriverStatistics = webDriverStats,
                ResourceStatistics = resourceStats,
                ApiStatistics = apiStats,
                PerformanceSummary = summary
            };
        }

        /// <summary>
        /// Gets optimization recommendations.
        /// </summary>
        public List<OptimizationRecommendation> GetRecommendations()
        {
            var webDriverStats = _webDriverMonitor.GetStatistics();
            var resourceStats = _resourceTracker.GetStatistics();
            var apiStats = _apiMonitor.GetOverallStats();

            return _optimizer.AnalyzeAndRecommend(webDriverStats, resourceStats, apiStats);
        }

        /// <summary>
        /// Clears all performance metrics.
        /// </summary>
        public void Clear()
        {
            _webDriverMonitor.Clear();
            _resourceTracker.Clear();
            _apiMonitor.Clear();
            Logger.Information("All performance metrics cleared");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _resourceTracker?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Comprehensive performance report.
    /// </summary>
    public class PerformanceReport
    {
        public DateTime Timestamp { get; set; }
        public WebDriverPerformanceStatistics WebDriverStatistics { get; set; } = new();
        public ResourceStatistics ResourceStatistics { get; set; } = new();
        public ApiPerformanceStats? ApiStatistics { get; set; }
        public PerformanceSummary PerformanceSummary { get; set; } = new();
    }
}

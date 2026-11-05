using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Performance
{
    /// <summary>
    /// Monitors WebDriver performance metrics including page load times, network timings, and JavaScript execution.
    /// </summary>
    public class WebDriverPerformanceMonitor
    {
        private readonly List<PageLoadMetrics> _pageLoadMetrics = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Records page load metrics for the current page.
        /// </summary>
        public PageLoadMetrics RecordPageLoad(IWebDriver driver, string? pageName = null)
        {
            try
            {
                var metrics = new PageLoadMetrics
                {
                    PageName = pageName ?? driver.Url,
                    Timestamp = DateTime.UtcNow,
                    Url = driver.Url
                };

                var navigationTiming = driver.ExecuteJavaScript<Dictionary<string, object>>(
                    "return window.performance.timing;");

                if (navigationTiming != null)
                {
                    metrics.DomContentLoaded = GetTimingValue(navigationTiming, "domContentLoadedEventEnd") - 
                                               GetTimingValue(navigationTiming, "navigationStart");
                    metrics.LoadComplete = GetTimingValue(navigationTiming, "loadEventEnd") - 
                                          GetTimingValue(navigationTiming, "navigationStart");
                    metrics.DomInteractive = GetTimingValue(navigationTiming, "domInteractive") - 
                                            GetTimingValue(navigationTiming, "navigationStart");
                    metrics.DomComplete = GetTimingValue(navigationTiming, "domComplete") - 
                                         GetTimingValue(navigationTiming, "navigationStart");
                    metrics.ResponseStart = GetTimingValue(navigationTiming, "responseStart") - 
                                          GetTimingValue(navigationTiming, "navigationStart");
                    metrics.ResponseEnd = GetTimingValue(navigationTiming, "responseEnd") - 
                                         GetTimingValue(navigationTiming, "navigationStart");
                }

                var resourceTiming = driver.ExecuteJavaScript<object[]>(
                    "return window.performance.getEntriesByType('resource');");

                if (resourceTiming != null && resourceTiming.Length > 0)
                {
                    metrics.ResourceCount = resourceTiming.Length;
                    metrics.TotalResourceSize = CalculateTotalResourceSize(resourceTiming);
                }

                try
                {
                    var paintTiming = driver.ExecuteJavaScript<Dictionary<string, object>>(
                        "return window.performance.getEntriesByType('paint');");
                    if (paintTiming != null)
                    {
                        var fcp = GetPaintTiming(paintTiming, "first-contentful-paint");
                        if (fcp > 0) metrics.FirstContentfulPaint = fcp;
                    }
                }
                catch
                {
                    // Paint timing may not be available in all browsers
                }

                lock (_lock)
                {
                    _pageLoadMetrics.Add(metrics);
                }

                Logger.Debug("Page load metrics recorded: {PageName}, Load Time: {LoadTime}ms", 
                    metrics.PageName, metrics.LoadComplete);

                return metrics;
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to record page load metrics: {Error}", ex.Message);
                return new PageLoadMetrics { PageName = pageName ?? "Unknown" };
            }
        }

        /// <summary>
        /// Gets performance statistics for all recorded page loads.
        /// </summary>
        public WebDriverPerformanceStatistics GetStatistics()
        {
            lock (_lock)
            {
                if (_pageLoadMetrics.Count == 0)
                {
                    return new WebDriverPerformanceStatistics();
                }

                var loadTimes = _pageLoadMetrics.Where(m => m.LoadComplete > 0).Select(m => m.LoadComplete).ToList();
                var domContentLoadedTimes = _pageLoadMetrics.Where(m => m.DomContentLoaded > 0).Select(m => m.DomContentLoaded).ToList();
                var resourceCounts = _pageLoadMetrics.Select(m => m.ResourceCount).ToList();

                return new WebDriverPerformanceStatistics
                {
                    TotalPageLoads = _pageLoadMetrics.Count,
                    AverageLoadTime = loadTimes.Any() ? loadTimes.Average() : 0,
                    MinLoadTime = loadTimes.Any() ? loadTimes.Min() : 0,
                    MaxLoadTime = loadTimes.Any() ? loadTimes.Max() : 0,
                    P95LoadTime = CalculatePercentile(loadTimes, 95),
                    P99LoadTime = CalculatePercentile(loadTimes, 99),
                    AverageDomContentLoaded = domContentLoadedTimes.Any() ? domContentLoadedTimes.Average() : 0,
                    AverageResourceCount = resourceCounts.Any() ? resourceCounts.Average() : 0,
                    PageLoadMetrics = _pageLoadMetrics.ToList()
                };
            }
        }

        /// <summary>
        /// Clears all recorded metrics.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _pageLoadMetrics.Clear();
                Logger.Information("WebDriver performance metrics cleared");
            }
        }

        private long GetTimingValue(Dictionary<string, object> timing, string key)
        {
            if (timing.TryGetValue(key, out var value) && value != null)
            {
                if (long.TryParse(value.ToString(), out var longValue))
                {
                    return longValue;
                }
            }
            return 0;
        }

        private long CalculateTotalResourceSize(object[] resources)
        {
            long totalSize = 0;
            foreach (var resource in resources)
            {
                if (resource is Dictionary<string, object> resourceDict)
                {
                    if (resourceDict.TryGetValue("transferSize", out var transferSize))
                    {
                        if (long.TryParse(transferSize?.ToString(), out var size))
                        {
                            totalSize += size;
                        }
                    }
                }
            }
            return totalSize;
        }

        private double GetPaintTiming(Dictionary<string, object> paintTiming, string paintName)
        {
            if (paintTiming.TryGetValue(paintName, out var value) && value != null)
            {
                if (double.TryParse(value.ToString(), out var timing))
                {
                    return timing;
                }
            }
            return 0;
        }

        private double CalculatePercentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return 0;
            values.Sort();
            var index = (int)Math.Ceiling(percentile / 100.0 * values.Count) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count - 1))];
        }
    }

    /// <summary>
    /// Page load performance metrics.
    /// </summary>
    public class PageLoadMetrics
    {
        public string PageName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double DomContentLoaded { get; set; }
        public double LoadComplete { get; set; }
        public double DomInteractive { get; set; }
        public double DomComplete { get; set; }
        public double ResponseStart { get; set; }
        public double ResponseEnd { get; set; }
        public double FirstContentfulPaint { get; set; }
        public int ResourceCount { get; set; }
        public long TotalResourceSize { get; set; }
    }

    /// <summary>
    /// WebDriver performance statistics.
    /// </summary>
    public class WebDriverPerformanceStatistics
    {
        public int TotalPageLoads { get; set; }
        public double AverageLoadTime { get; set; }
        public double MinLoadTime { get; set; }
        public double MaxLoadTime { get; set; }
        public double P95LoadTime { get; set; }
        public double P99LoadTime { get; set; }
        public double AverageDomContentLoaded { get; set; }
        public double AverageResourceCount { get; set; }
        public List<PageLoadMetrics> PageLoadMetrics { get; set; } = new();
    }
}

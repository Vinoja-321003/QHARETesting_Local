using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.API;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Performance
{
    /// <summary>
    /// Analyzes performance metrics and provides optimization recommendations.
    /// </summary>
    public class PerformanceOptimizer
    {
        /// <summary>
        /// Analyzes performance data and generates optimization recommendations.
        /// </summary>
        public List<OptimizationRecommendation> AnalyzeAndRecommend(
            WebDriverPerformanceStatistics webDriverStats,
            ResourceStatistics resourceStats,
            ApiPerformanceStats? apiStats = null)
        {
            var recommendations = new List<OptimizationRecommendation>();

            if (webDriverStats.TotalPageLoads > 0)
            {
                if (webDriverStats.AverageLoadTime > 5000)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "Page Load Performance",
                        Severity = RecommendationSeverity.High,
                        Issue = $"Average page load time is {webDriverStats.AverageLoadTime:F2}ms (threshold: 5000ms)",
                        Recommendation = "Optimize page load: reduce resource size, enable caching, minimize JavaScript execution",
                        Impact = "High - Slow page loads affect user experience"
                    });
                }

                if (webDriverStats.AverageResourceCount > 100)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "Resource Optimization",
                        Severity = RecommendationSeverity.Medium,
                        Issue = $"Average resource count is {webDriverStats.AverageResourceCount:F0} (threshold: 100)",
                        Recommendation = "Reduce number of resources: combine CSS/JS files, use sprites for images, lazy load resources",
                        Impact = "Medium - High resource count increases load time"
                    });
                }

                if (webDriverStats.AverageDomContentLoaded > 3000)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "DOM Performance",
                        Severity = RecommendationSeverity.Medium,
                        Issue = $"Average DOM content loaded time is {webDriverStats.AverageDomContentLoaded:F2}ms (threshold: 3000ms)",
                        Recommendation = "Optimize DOM: reduce DOM size, defer non-critical scripts, optimize CSS delivery",
                        Impact = "Medium - Slow DOM affects perceived performance"
                    });
                }
            }

            if (resourceStats.TotalSnapshots > 0)
            {
                if (resourceStats.AverageCpuUsage > 80)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "CPU Utilization",
                        Severity = RecommendationSeverity.High,
                        Issue = $"Average CPU usage is {resourceStats.AverageCpuUsage:F2}% (threshold: 80%)",
                        Recommendation = "Reduce CPU usage: optimize test execution, reduce parallel tests, optimize wait strategies",
                        Impact = "High - High CPU usage can cause test instability"
                    });
                }

                if (resourceStats.AverageMemoryUsage > 2000)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "Memory Utilization",
                        Severity = RecommendationSeverity.Medium,
                        Issue = $"Average memory usage is {resourceStats.AverageMemoryUsage:F2}MB (threshold: 2000MB)",
                        Recommendation = "Reduce memory usage: dispose WebDriver instances properly, clear caches, reduce test data size",
                        Impact = "Medium - High memory usage can cause system slowdown"
                    });
                }
            }

            if (apiStats != null && apiStats.TotalCalls > 0)
            {
                if (apiStats.AverageDuration.TotalMilliseconds > 1000)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "API Performance",
                        Severity = RecommendationSeverity.Medium,
                        Issue = $"Average API response time is {apiStats.AverageDuration.TotalMilliseconds:F2}ms (threshold: 1000ms)",
                        Recommendation = "Optimize API calls: implement caching, reduce payload size, optimize queries",
                        Impact = "Medium - Slow API calls affect test execution time"
                    });
                }

                if (apiStats.SuccessRate < 95)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Category = "API Reliability",
                        Severity = RecommendationSeverity.High,
                        Issue = $"API success rate is {apiStats.SuccessRate:F2}% (threshold: 95%)",
                        Recommendation = "Improve API reliability: add retry logic, implement circuit breaker, check API health",
                        Impact = "High - Low success rate causes test failures"
                    });
                }
            }

            Logger.Information("Generated {Count} performance optimization recommendations", recommendations.Count);
            return recommendations;
        }

        /// <summary>
        /// Gets a summary of performance analysis.
        /// </summary>
        public PerformanceSummary GetSummary(
            WebDriverPerformanceStatistics webDriverStats,
            ResourceStatistics resourceStats,
            ApiPerformanceStats? apiStats = null)
        {
            var recommendations = AnalyzeAndRecommend(webDriverStats, resourceStats, apiStats);

            return new PerformanceSummary
            {
                OverallScore = CalculateOverallScore(webDriverStats, resourceStats, apiStats),
                TotalRecommendations = recommendations.Count,
                HighPriorityRecommendations = recommendations.Count(r => r.Severity == RecommendationSeverity.High),
                MediumPriorityRecommendations = recommendations.Count(r => r.Severity == RecommendationSeverity.Medium),
                LowPriorityRecommendations = recommendations.Count(r => r.Severity == RecommendationSeverity.Low),
                Recommendations = recommendations
            };
        }

        private int CalculateOverallScore(
            WebDriverPerformanceStatistics webDriverStats,
            ResourceStatistics resourceStats,
            ApiPerformanceStats? apiStats)
        {
            int score = 100;

            if (webDriverStats.AverageLoadTime > 5000) score -= 20;
            if (webDriverStats.AverageLoadTime > 10000) score -= 20;
            if (resourceStats.AverageCpuUsage > 80) score -= 15;
            if (resourceStats.AverageMemoryUsage > 2000) score -= 10;
            if (apiStats != null && apiStats.AverageDuration.TotalMilliseconds > 1000) score -= 10;
            if (apiStats != null && apiStats.SuccessRate < 95) score -= 15;

            return Math.Max(0, Math.Min(100, score));
        }
    }

    /// <summary>
    /// Performance optimization recommendation.
    /// </summary>
    public class OptimizationRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public RecommendationSeverity Severity { get; set; }
        public string Issue { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    /// <summary>
    /// Recommendation severity levels.
    /// </summary>
    public enum RecommendationSeverity
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Performance analysis summary.
    /// </summary>
    public class PerformanceSummary
    {
        public int OverallScore { get; set; }
        public int TotalRecommendations { get; set; }
        public int HighPriorityRecommendations { get; set; }
        public int MediumPriorityRecommendations { get; set; }
        public int LowPriorityRecommendations { get; set; }
        public List<OptimizationRecommendation> Recommendations { get; set; } = new();
    }
}

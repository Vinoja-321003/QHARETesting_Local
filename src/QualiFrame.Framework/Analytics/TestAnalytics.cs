using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Analytics
{
    /// <summary>
    /// Comprehensive test analytics and reporting.
    /// </summary>
    public class TestAnalytics
    {
        private readonly TestMetrics _metrics;
        private readonly TestConfiguration _config;
        private readonly string _analyticsPath;

        public TestAnalytics()
        {
            _metrics = new TestMetrics();
            _config = TestConfiguration.Instance;
            _analyticsPath = Path.Combine(Directory.GetCurrentDirectory(), "Results", "Analytics");
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Records a test execution.
        /// </summary>
        public void RecordTestExecution(string testName, string status, TimeSpan duration, string? errorMessage = null)
        {
            _metrics.RecordExecution(testName, status, duration, errorMessage);
        }

        /// <summary>
        /// Generates analytics report.
        /// </summary>
        public AnalyticsReport GenerateReport()
        {
            var statistics = _metrics.GetStatistics();
            var failedTests = _metrics.GetFailedTests();
            var slowestTests = _metrics.GetSlowestTests(10);
            var trend = CalculateTrend();

            var report = new AnalyticsReport
            {
                GeneratedAt = DateTime.UtcNow,
                Statistics = statistics,
                FailedTests = failedTests,
                SlowestTests = slowestTests,
                Trend = trend,
                TestExecutionHistory = _metrics.GetAllExecutions()
            };

            SaveReport(report);
            Logger.Information("Analytics report generated: {TotalTests} tests, {SuccessRate:P2} success rate",
                statistics.TotalTests, statistics.SuccessRate / 100);

            return report;
        }

        /// <summary>
        /// Generates trend analysis.
        /// </summary>
        public TrendAnalysis CalculateTrend()
        {
            var executions = _metrics.GetAllExecutions();
            if (executions.Count < 2)
            {
                return new TrendAnalysis
                {
                    IsImproving = false,
                    TrendDescription = "Insufficient data for trend analysis"
                };
            }

            var dailyStats = executions
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Passed = g.Count(e => e.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase)),
                    SuccessRate = (double)g.Count(e => e.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase)) / g.Count() * 100
                })
                .OrderBy(x => x.Date)
                .ToList();

            if (dailyStats.Count < 2)
            {
                return new TrendAnalysis
                {
                    IsImproving = false,
                    TrendDescription = "Insufficient data for trend analysis"
                };
            }

            var recentSuccessRate = dailyStats.TakeLast(3).Average(x => x.SuccessRate);
            var previousSuccessRate = dailyStats.SkipLast(3).TakeLast(3).Any() 
                ? dailyStats.SkipLast(3).TakeLast(3).Average(x => x.SuccessRate)
                : recentSuccessRate;

            var isImproving = recentSuccessRate > previousSuccessRate;
            var change = recentSuccessRate - previousSuccessRate;

            return new TrendAnalysis
            {
                IsImproving = isImproving,
                RecentSuccessRate = recentSuccessRate,
                PreviousSuccessRate = previousSuccessRate,
                ChangePercentage = change,
                TrendDescription = isImproving
                    ? $"Test success rate improved by {change:F2}%"
                    : $"Test success rate decreased by {Math.Abs(change):F2}%"
            };
        }

        /// <summary>
        /// Exports analytics data to JSON.
        /// </summary>
        public void ExportToJson(string? filePath = null)
        {
            var report = GenerateReport();
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            filePath ??= Path.Combine(_analyticsPath, $"analytics_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(filePath, json);
            Logger.Information("Analytics data exported to: {Path}", filePath);
        }

        /// <summary>
        /// Gets metrics instance for direct access.
        /// </summary>
        public TestMetrics Metrics => _metrics;

        private void SaveReport(AnalyticsReport report)
        {
            try
            {
                var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(_analyticsPath, fileName);

                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);
                Logger.Debug("Analytics report saved: {Path}", filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save analytics report: {Error}", ex.Message);
            }
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                Directory.CreateDirectory(_analyticsPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create analytics directory: {Error}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Analytics report.
    /// </summary>
    public class AnalyticsReport
    {
        public DateTime GeneratedAt { get; set; }
        public TestStatistics Statistics { get; set; } = new TestStatistics();
        public List<TestExecutionRecord> FailedTests { get; set; } = new List<TestExecutionRecord>();
        public List<TestExecutionRecord> SlowestTests { get; set; } = new List<TestExecutionRecord>();
        public TrendAnalysis Trend { get; set; } = new TrendAnalysis();
        public List<TestExecutionRecord> TestExecutionHistory { get; set; } = new List<TestExecutionRecord>();
    }

    /// <summary>
    /// Trend analysis.
    /// </summary>
    public class TrendAnalysis
    {
        public bool IsImproving { get; set; }
        public double RecentSuccessRate { get; set; }
        public double PreviousSuccessRate { get; set; }
        public double ChangePercentage { get; set; }
        public string TrendDescription { get; set; } = string.Empty;
    }
}

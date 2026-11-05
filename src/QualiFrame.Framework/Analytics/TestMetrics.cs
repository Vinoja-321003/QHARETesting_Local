using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Analytics
{
    /// <summary>
    /// Tracks and aggregates test execution metrics.
    /// </summary>
    public class TestMetrics
    {
        private readonly List<TestExecutionRecord> _executions;
        private readonly object _lockObject = new object();

        public TestMetrics()
        {
            _executions = new List<TestExecutionRecord>();
        }

        /// <summary>
        /// Records a test execution.
        /// </summary>
        public void RecordExecution(string testName, string status, TimeSpan duration, string? errorMessage = null)
        {
            lock (_lockObject)
            {
                _executions.Add(new TestExecutionRecord
                {
                    TestName = testName,
                    Status = status,
                    Duration = duration,
                    ErrorMessage = errorMessage,
                    Timestamp = DateTime.UtcNow
                });

                Logger.Debug("Test execution recorded: {TestName}, Status: {Status}, Duration: {Duration}ms",
                    testName, status, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Gets overall test statistics.
        /// </summary>
        public TestStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                var total = _executions.Count;
                var passed = _executions.Count(e => e.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase));
                var failed = _executions.Count(e => e.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
                var skipped = _executions.Count(e => e.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase));

                var durations = _executions.Select(e => e.Duration.TotalMilliseconds).ToList();
                var avgDuration = durations.Any() ? durations.Average() : 0;
                var minDuration = durations.Any() ? durations.Min() : 0;
                var maxDuration = durations.Any() ? durations.Max() : 0;

                var successRate = total > 0 ? (double)passed / total * 100 : 0;

                return new TestStatistics
                {
                    TotalTests = total,
                    Passed = passed,
                    Failed = failed,
                    Skipped = skipped,
                    SuccessRate = successRate,
                    AverageDuration = TimeSpan.FromMilliseconds(avgDuration),
                    MinDuration = TimeSpan.FromMilliseconds(minDuration),
                    MaxDuration = TimeSpan.FromMilliseconds(maxDuration),
                    TotalDuration = TimeSpan.FromMilliseconds(durations.Sum())
                };
            }
        }

        /// <summary>
        /// Gets statistics for a specific time range.
        /// </summary>
        public TestStatistics GetStatistics(DateTime startTime, DateTime endTime)
        {
            lock (_lockObject)
            {
                var filtered = _executions.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList();
                
                var total = filtered.Count;
                var passed = filtered.Count(e => e.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase));
                var failed = filtered.Count(e => e.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
                var skipped = filtered.Count(e => e.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase));

                var durations = filtered.Select(e => e.Duration.TotalMilliseconds).ToList();
                var avgDuration = durations.Any() ? durations.Average() : 0;
                var minDuration = durations.Any() ? durations.Min() : 0;
                var maxDuration = durations.Any() ? durations.Max() : 0;

                var successRate = total > 0 ? (double)passed / total * 100 : 0;

                return new TestStatistics
                {
                    TotalTests = total,
                    Passed = passed,
                    Failed = failed,
                    Skipped = skipped,
                    SuccessRate = successRate,
                    AverageDuration = TimeSpan.FromMilliseconds(avgDuration),
                    MinDuration = TimeSpan.FromMilliseconds(minDuration),
                    MaxDuration = TimeSpan.FromMilliseconds(maxDuration),
                    TotalDuration = TimeSpan.FromMilliseconds(durations.Sum())
                };
            }
        }

        /// <summary>
        /// Gets failed test details.
        /// </summary>
        public List<TestExecutionRecord> GetFailedTests()
        {
            lock (_lockObject)
            {
                return _executions
                    .Where(e => e.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.Timestamp)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets slowest tests.
        /// </summary>
        public List<TestExecutionRecord> GetSlowestTests(int count = 10)
        {
            lock (_lockObject)
            {
                return _executions
                    .OrderByDescending(e => e.Duration)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Clears all metrics.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _executions.Clear();
                Logger.Information("Test metrics cleared");
            }
        }

        /// <summary>
        /// Gets all execution records.
        /// </summary>
        public List<TestExecutionRecord> GetAllExecutions()
        {
            lock (_lockObject)
            {
                return _executions.ToList();
            }
        }
    }

    /// <summary>
    /// Represents a test execution record.
    /// </summary>
    public class TestExecutionRecord
    {
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Test execution statistics.
    /// </summary>
    public class TestStatistics
    {
        public int TotalTests { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}

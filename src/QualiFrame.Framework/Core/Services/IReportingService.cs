using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QualiFrame.Framework.Core.Services
{
    /// <summary>
    /// Service interface for test reporting operations.
    /// </summary>
    public interface IReportingService
    {
        /// <summary>
        /// Generates a test execution report.
        /// </summary>
        Task GenerateReportAsync(IEnumerable<TestResult> testResults, string outputPath);

        /// <summary>
        /// Adds a screenshot to the current test report.
        /// </summary>
        void AddScreenshot(string screenshotPath, string description = "");

        /// <summary>
        /// Adds a log entry to the current test report.
        /// </summary>
        void AddLog(LogLevel level, string message);

        /// <summary>
        /// Flushes the report to disk.
        /// </summary>
        Task FlushAsync();
    }

    /// <summary>
    /// Represents a test result.
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// Log levels for reporting.
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }
}

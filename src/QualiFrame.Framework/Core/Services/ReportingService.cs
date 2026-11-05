using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Reporting;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualiFrame.Framework.Core.Services
{
    /// <summary>
    /// Service for test reporting operations.
    /// </summary>
    public class ReportingService : IReportingService
    {
        private readonly ExtentReportManager _extentReportManager;
        private readonly AllureReporter _allureReporter;
        private readonly ReportGenerator _reportGenerator;

        public ReportingService(
            ExtentReportManager extentReportManager,
            AllureReporter allureReporter,
            ReportGenerator reportGenerator)
        {
            _extentReportManager = extentReportManager ?? throw new ArgumentNullException(nameof(extentReportManager));
            _allureReporter = allureReporter ?? throw new ArgumentNullException(nameof(allureReporter));
            _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
        }

        /// <summary>
        /// Generates a test execution report.
        /// </summary>
        public async Task GenerateReportAsync(IEnumerable<TestResult> testResults, string outputPath)
        {
            if (testResults == null)
                throw new ArgumentNullException(nameof(testResults));
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            Logger.Information("Generating test report at: {OutputPath}", outputPath);

            try
            {
                var resultsList = testResults.ToList();
                await Task.Run(() =>
                {
                    var htmlContent = GenerateHtmlReport(resultsList);
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(resultsList);
                    
                    File.WriteAllText(
                        Path.Combine(outputPath, "test-report.html"), 
                        htmlContent);
                    File.WriteAllText(
                        Path.Combine(outputPath, "test-report.json"), 
                        jsonContent);
                });

                Logger.Information("Test report generated successfully. Total tests: {Count}", resultsList.Count);
            }
            catch (Exception ex)
            {
                Logger.Error("Error generating test report: {Error}", ex.Message);
                throw;
            }
        }

        private string GenerateHtmlReport(List<TestResult> results)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><title>Test Report</title>");
            html.AppendLine("<style>table{border-collapse:collapse;width:100%;}th,td{border:1px solid #ddd;padding:8px;text-align:left;}th{background-color:#4CAF50;color:white;}</style>");
            html.AppendLine("</head><body>");
            html.AppendLine("<h1>Test Execution Report</h1>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Test Name</th><th>Status</th><th>Duration (ms)</th><th>Error</th></tr>");
            
            foreach (var result in results)
            {
                html.AppendLine($"<tr><td>{result.TestName}</td><td>{result.Status}</td><td>{result.DurationMs}</td><td>{result.ErrorMessage ?? ""}</td></tr>");
            }
            
            html.AppendLine("</table>");
            html.AppendLine("</body></html>");
            return html.ToString();
        }

        /// <summary>
        /// Adds a screenshot to the current test report.
        /// </summary>
        public void AddScreenshot(string screenshotPath, string description = "")
        {
            if (string.IsNullOrEmpty(screenshotPath))
                throw new ArgumentException("Screenshot path cannot be null or empty", nameof(screenshotPath));

            try
            {
                ExtentReportManager.AddScreenshot(screenshotPath, description);
                if (File.Exists(screenshotPath))
                {
                    var screenshotBytes = File.ReadAllBytes(screenshotPath);
                    _allureReporter.AddAttachment("Screenshot", screenshotBytes, "image/png");
                }
                Logger.Debug("Screenshot added to report: {Path}", screenshotPath);
            }
            catch (Exception ex)
            {
                Logger.Warning("Error adding screenshot to report: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Adds a log entry to the current test report.
        /// </summary>
        public void AddLog(LogLevel level, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                switch (level)
                {
                    case LogLevel.Info:
                        ExtentReportManager.LogInfo(message);
                        Logger.Information(message);
                        break;
                    case LogLevel.Warning:
                        ExtentReportManager.LogWarning(message);
                        Logger.Warning(message);
                        break;
                    case LogLevel.Error:
                        ExtentReportManager.LogError(message);
                        Logger.Error(message);
                        break;
                    case LogLevel.Debug:
                        Logger.Debug(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error adding log to report: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Flushes the report to disk.
        /// </summary>
        public async Task FlushAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    ExtentReportManager.Flush();
                });
                Logger.Information("Report flushed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error flushing report: {Error}", ex.Message);
                throw;
            }
        }
    }
}

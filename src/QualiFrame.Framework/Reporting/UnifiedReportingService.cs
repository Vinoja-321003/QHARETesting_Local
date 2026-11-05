using System;
using System.IO;
using System.Threading;
using AventStack.ExtentReports;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Reporting
{
    /// <summary>
    /// Unified reporting service that coordinates Extent and Allure reporting.
    /// Provides a single API for both reporting systems, similar to Java framework's unified approach.
    /// Thread-safe using AsyncLocal for parallel execution support.
    /// </summary>
    public class UnifiedReportingService
    {
        // Thread-local storage for service instance (one per thread)
        private static readonly AsyncLocal<UnifiedReportingService?> _instance = new AsyncLocal<UnifiedReportingService?>();
        
        private readonly TestConfiguration _config;
        private readonly EnhancedAllureReporter _allureReporter;

        /// <summary>
        /// Gets the current thread's reporting service instance.
        /// </summary>
        public static UnifiedReportingService GetInstance()
        {
            if (_instance.Value == null)
            {
                _instance.Value = new UnifiedReportingService();
            }
            return _instance.Value;
        }

        private UnifiedReportingService()
        {
            _config = TestConfiguration.Instance;
            _allureReporter = EnhancedAllureReporter.GetInstance();
        }

        /// <summary>
        /// Starts a feature in both reporting systems.
        /// </summary>
        public void StartFeature(string featureName)
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    ExtentReportManager.CreateFeature(featureName);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to start feature in ExtentReports: {Error}", ex.Message);
                }
            }
            // Allure doesn't have explicit feature concept, handled via labels
        }

        /// <summary>
        /// Starts a test case/scenario in both reporting systems.
        /// </summary>
        public void StartTestCase(string testName, string featureTitle, string[]? tags = null)
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    ExtentReportManager.CreateScenario(featureTitle, testName);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to start test case in ExtentReports: {Error}", ex.Message);
                }
            }

            if (_config.AllureReportEnabled)
            {
                try
                {
                    _allureReporter.StartTestCase(testName, featureTitle, tags);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to start test case in Allure: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Logs a step in both reporting systems.
        /// </summary>
        public void LogStep(string stepName, string message, bool passed = true)
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    ExtentReportManager.CreateStep(stepName);
                    var status = passed ? Status.Pass : Status.Fail;
                    ExtentReportManager.Log(status, message);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to log step in ExtentReports: {Error}", ex.Message);
                }
            }

            if (_config.AllureReportEnabled)
            {
                try
                {
                    _allureReporter.StartStep(stepName);
                    _allureReporter.CompleteStep(passed);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to log step in Allure: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Logs a step with custom status string (supports Java-style status mapping).
        /// </summary>
        public void LogStep(string stepName, string message, string status)
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    ExtentReportManager.CreateStep(stepName);
                    var mappedStatus = ExtentReportStatusMapper.MapStatus(status);
                    ExtentReportManager.Log(mappedStatus, message);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to log step in ExtentReports: {Error}", ex.Message);
                }
            }

            if (_config.AllureReportEnabled)
            {
                try
                {
                    var passed = !status.Contains("FAIL", StringComparison.OrdinalIgnoreCase);
                    _allureReporter.StartStep(stepName);
                    _allureReporter.CompleteStep(passed);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to log step in Allure: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Completes a test case in both reporting systems.
        /// </summary>
        public void CompleteTestCase(bool passed, Exception? exception = null)
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    if (passed)
                    {
                        ExtentReportManager.MarkScenarioAsPassed();
                    }
                    else
                    {
                        var message = exception?.Message ?? "Test case failed";
                        ExtentReportManager.MarkScenarioAsFailed(message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to complete test case in ExtentReports: {Error}", ex.Message);
                }
            }

            if (_config.AllureReportEnabled)
            {
                try
                {
                    _allureReporter.CompleteTestCase(passed, exception);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to complete test case in Allure: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Adds a screenshot to both reporting systems.
        /// </summary>
        public void AddScreenshot(string screenshotPath, string title = "Screenshot")
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    ExtentReportManager.AddScreenshot(screenshotPath, title);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to add screenshot to ExtentReports: {Error}", ex.Message);
                }
            }

            if (_config.AllureReportEnabled && File.Exists(screenshotPath))
            {
                try
                {
                    _allureReporter.AddScreenshotFromFile(screenshotPath, title);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to add screenshot to Allure: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Adds a screenshot from byte array to both reporting systems.
        /// </summary>
        public void AddScreenshot(byte[] screenshotBytes, string name = "Screenshot")
        {
            if (_config.AllureReportEnabled)
            {
                try
                {
                    _allureReporter.AddScreenshot(name, screenshotBytes);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to add screenshot to Allure: {Error}", ex.Message);
                }
            }

            // For ExtentReports, we need to save to file first
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid()}.png");
                    File.WriteAllBytes(tempPath, screenshotBytes);
                    ExtentReportManager.AddScreenshot(tempPath, name);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to add screenshot to ExtentReports: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Flushes all reports.
        /// </summary>
        public void Flush()
        {
            if (_config.ExtentReportEnabled)
            {
                try
                {
                    ExtentReportManager.Flush();
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to flush ExtentReports: {Error}", ex.Message);
                }
            }
            // Allure doesn't require explicit flush - files are written automatically
        }

        /// <summary>
        /// Logs an info message to both reporting systems.
        /// </summary>
        public void LogInfo(string message)
        {
            if (_config.ExtentReportEnabled)
            {
                ExtentReportManager.LogInfo(message);
            }
            // Allure doesn't have direct info logging, use step logging
        }

        /// <summary>
        /// Logs a warning message to both reporting systems.
        /// </summary>
        public void LogWarning(string message)
        {
            if (_config.ExtentReportEnabled)
            {
                ExtentReportManager.LogWarning(message);
            }
            // Allure doesn't have direct warning logging, use step logging
        }

        /// <summary>
        /// Logs an error message to both reporting systems.
        /// </summary>
        public void LogError(string message)
        {
            if (_config.ExtentReportEnabled)
            {
                ExtentReportManager.LogError(message);
            }
            // Allure doesn't have direct error logging, use step logging
        }
    }
}

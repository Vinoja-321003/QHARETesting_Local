using System;
using System.IO;
using System.Threading;
using Allure.Net.Commons;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;
using Newtonsoft.Json;

namespace QualiFrame.Framework.Reporting
{
    /// <summary>
    /// Enhanced Allure reporter with simplified API and automatic result management.
    /// Reduces complexity compared to manual JSON writing approach.
    /// Implements best practices from Java framework's automatic Allure integration.
    /// </summary>
    public class EnhancedAllureReporter
    {
        // Thread-local storage for test context (equivalent to Java's thread-local approach)
        private static readonly AsyncLocal<string?> _currentTestUuid = new AsyncLocal<string?>();
        private static readonly AsyncLocal<string?> _currentFeatureTitle = new AsyncLocal<string?>();
        private static readonly AsyncLocal<StepResult?> _currentStep = new AsyncLocal<StepResult?>();
        
        private readonly TestConfiguration _config;
        private readonly string _allureResultsPath;

        /// <summary>
        /// Gets the singleton instance for the current thread.
        /// </summary>
        private static EnhancedAllureReporter? _instance;
        private static readonly object _instanceLock = new object();

        public static EnhancedAllureReporter GetInstance()
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new EnhancedAllureReporter();
                    }
                }
            }
            return _instance;
        }

        private EnhancedAllureReporter()
        {
            _config = TestConfiguration.Instance;
            _allureResultsPath = ResolveAllureResultsPath();
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Starts a new test case (thread-safe).
        /// </summary>
        public void StartTestCase(string testName, string featureTitle, string[]? tags = null)
        {
            try
            {
                var testUuid = Guid.NewGuid().ToString();
                _currentTestUuid.Value = testUuid; // Thread-local storage
                _currentFeatureTitle.Value = featureTitle; // Thread-local storage

                var testResult = new TestResult
                {
                    uuid = testUuid,
                    name = testName,
                    fullName = $"{featureTitle}.{testName}",
                    historyId = testName.GetHashCode().ToString(),
                    status = Status.none,
                    start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                testResult.labels.Add(new Label { name = "feature", value = featureTitle });
                
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        testResult.labels.Add(new Label { name = "tag", value = tag });
                    }
                }

                AllureLifecycle.Instance.StartTestCase(testResult);
                Logger.Debug("Allure test case started: {Uuid} - {TestName}", testUuid, testName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start Allure test case: {TestName}", testName);
            }
        }

        /// <summary>
        /// Completes the current test case (thread-safe).
        /// </summary>
        public void CompleteTestCase(bool passed, Exception? exception = null)
        {
            try
            {
                var testUuid = _currentTestUuid.Value;
                if (string.IsNullOrEmpty(testUuid))
                {
                    Logger.Warning("No active Allure test case to complete");
                    return;
                }

                AllureLifecycle.Instance.UpdateTestCase(tc =>
                {
                    if (tc.uuid == testUuid)
                    {
                        tc.status = passed ? Status.passed : Status.failed;
                        tc.stage = Stage.finished;
                        tc.stop = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        if (exception != null)
                        {
                            tc.statusDetails ??= new StatusDetails();
                            tc.statusDetails.message = exception.Message;
                            tc.statusDetails.trace = exception.StackTrace ?? "";
                        }
                    }
                });

                AllureLifecycle.Instance.StopTestCase();
                _currentTestUuid.Value = null; // Clear thread-local storage
                Logger.Debug("Allure test case completed: {Uuid}, Status: {Status}", testUuid, passed ? "passed" : "failed");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to complete Allure test case: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Starts a step (thread-safe).
        /// </summary>
        public void StartStep(string stepName)
        {
            try
            {
                var stepResult = new StepResult { name = stepName };
                _currentStep.Value = stepResult; // Thread-local storage
                AllureLifecycle.Instance.StartStep(stepResult);
                Logger.Debug("Allure step started: {StepName}", stepName);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to start Allure step: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Completes the current step (thread-safe).
        /// </summary>
        public void CompleteStep(bool passed, Exception? exception = null)
        {
            try
            {
                if (_currentStep.Value == null)
                {
                    return;
                }

                AllureLifecycle.Instance.UpdateStep(step =>
                {
                    step.status = passed ? Status.passed : Status.failed;
                    if (exception != null)
                    {
                        step.statusDetails = new StatusDetails
                        {
                            message = exception.Message,
                            trace = exception.StackTrace
                        };
                    }
                });

                AllureLifecycle.Instance.StopStep();
                _currentStep.Value = null; // Clear thread-local storage
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to complete Allure step: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Adds an attachment (thread-safe).
        /// </summary>
        public void AddAttachment(string name, byte[] content, string contentType = "application/octet-stream")
        {
            try
            {
                AllureApi.AddAttachment(name, contentType, content);
                Logger.Debug("Allure attachment added: {Name}", name);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to add Allure attachment: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Adds a screenshot attachment (thread-safe).
        /// </summary>
        public void AddScreenshot(string name, byte[] screenshot)
        {
            AddAttachment($"{name}.png", screenshot, "image/png");
        }

        /// <summary>
        /// Adds a screenshot from file path.
        /// </summary>
        public void AddScreenshotFromFile(string screenshotPath, string name = "Screenshot")
        {
            try
            {
                if (File.Exists(screenshotPath))
                {
                    var screenshotBytes = File.ReadAllBytes(screenshotPath);
                    AddScreenshot(name, screenshotBytes);
                }
                else
                {
                    Logger.Warning("Screenshot file not found: {Path}", screenshotPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to add screenshot from file: {Path}", screenshotPath);
            }
        }

        private string ResolveAllureResultsPath()
        {
            // Check environment variable first (takes precedence)
            var envPath = Environment.GetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return Path.GetFullPath(envPath);
            }

            // Use configured path
            var configPath = _config.AllureResultsPath;
            if (Path.IsPathRooted(configPath))
            {
                return Path.GetFullPath(configPath);
            }

            // Resolve relative to project root
            var projectRoot = FindProjectRoot();
            return Path.GetFullPath(Path.Combine(projectRoot, configPath));
        }

        private string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);
            
            while (dir != null && dir.Exists)
            {
                if (dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            
            return currentDir;
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_allureResultsPath))
                {
                    Directory.CreateDirectory(_allureResultsPath);
                    Logger.Information("Created Allure results directory: {Path}", _allureResultsPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create Allure results directory: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Gets the Allure results directory path.
        /// </summary>
        public string GetResultsPath()
        {
            return _allureResultsPath;
        }
    }
}

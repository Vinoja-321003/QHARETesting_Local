using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Framework.Data.Repositories;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Reporting;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;
using ReportingLogLevel = QualiFrame.Framework.Core.Services.LogLevel;

namespace QualiFrame.Framework.Core
{
    /// <summary>
    /// Base class for modular test cases following Template Method pattern.
    /// Provides a structured approach to test execution with setup, execution, and teardown phases.
    /// </summary>
    public abstract class ModularTestCase
    {
        /// <summary>
        /// Gets the data repository for test data access.
        /// </summary>
        protected IDataRepository? DataRepository { get; private set; }

        /// <summary>
        /// Gets the reporting service for test reporting.
        /// </summary>
        protected IReportingService? ReportingService { get; private set; }

        /// <summary>
        /// Gets the test execution service for test orchestration.
        /// </summary>
        protected ITestExecutionService? TestExecutionService { get; private set; }

        /// <summary>
        /// Gets the WebDriver instance.
        /// </summary>
        protected IWebDriver? Driver { get; private set; }

        /// <summary>
        /// Gets the test configuration.
        /// </summary>
        protected TestConfiguration Config { get; private set; } = null!;

        /// <summary>
        /// Gets the current test name.
        /// </summary>
        protected string TestName => TestContext.CurrentContext.Test.Name;

        /// <summary>
        /// Gets the current test description.
        /// </summary>
        protected string? TestDescription => TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString();

        /// <summary>
        /// Base setup method that initializes framework components.
        /// Derived classes should override SetUp() for custom setup logic.
        /// </summary>
        [SetUp]
        public void BaseSetUp()
        {
            try
            {
                Logger.Information("Setting up test: {TestName}", TestName);

                // Initialize configuration
                Config = TestConfiguration.Instance;

                // Initialize data repository
                DataRepository = DataRepositoryFactory.CreateRepository();

                // Initialize services
                var extentReportManager = new ExtentReportManager();
                var allureReporter = new AllureReporter();
                var reportGenerator = new ReportGenerator();
                ReportingService = new ReportingService(extentReportManager, allureReporter, reportGenerator);
                
                var testMetrics = new TestMetrics();
                TestExecutionService = new TestExecutionService(testMetrics);

                // Initialize WebDriver
                Driver = EnhancedWebDriverFactory.CreateDriver();
                EnhancedWebDriverFactory.SetCurrentDriver(Driver);

                // Configure driver
                ConfigureDriver(Driver);

                // Navigate to base URL if configured
                if (!string.IsNullOrEmpty(Config.BaseUrl))
                {
                    Driver.Navigate().GoToUrl(Config.BaseUrl);
                    Logger.Debug("Navigated to base URL: {BaseUrl}", Config.BaseUrl);
                }

                // Call derived class setup
                SetUp();

                Logger.Information("Test setup completed: {TestName}", TestName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Test setup failed: {TestName}", TestName);
                throw;
            }
        }

        /// <summary>
        /// Base teardown method that cleans up framework components.
        /// Derived classes should override TearDown() for custom teardown logic.
        /// </summary>
        [TearDown]
        public void BaseTearDown()
        {
            try
            {
                Logger.Information("Tearing down test: {TestName}", TestName);

                // Call derived class teardown
                TearDown();

                // Cleanup WebDriver
                if (Driver != null)
                {
                    try
                    {
                        VideoRecordingManager.Stop();
                        Driver.Quit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Error quitting driver: {Message}", ex.Message);
                    }
                    finally
                    {
                        Driver.Dispose();
                        Driver = null;
                    }
                }

                Logger.Information("Test teardown completed: {TestName}", TestName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Test teardown failed: {TestName}", TestName);
                // Don't throw in teardown to allow cleanup to complete
            }
        }

        /// <summary>
        /// Main test method that orchestrates test execution using Template Method pattern.
        /// </summary>
        [Test]
        public void RunTest()
        {
            if (Driver == null)
            {
                throw new InvalidOperationException("WebDriver is not initialized. Ensure BaseSetUp() is called.");
            }

            try
            {
                Logger.Information("Starting test execution: {TestName}", TestName);

                // Start reporting
                ReportingService?.AddLog(ReportingLogLevel.Info, $"Test started: {TestName}");

                // Execute test
                ExecuteTest();

                // End reporting with success
                ReportingService?.AddLog(ReportingLogLevel.Info, $"Test passed: {TestName}");

                Logger.Information("Test execution completed successfully: {TestName}", TestName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Test execution failed: {TestName}", TestName);

                // Take screenshot on failure if enabled
                if (Config.ScreenshotOnFailure && Driver != null)
                {
                    try
                    {
                        TakeScreenshot(TestName);
                    }
                    catch (Exception screenshotEx)
                    {
                        Logger.Warning("Failed to take screenshot on test failure: {Error}", screenshotEx.Message);
                    }
                }

                // End reporting with failure
                ReportingService?.AddLog(ReportingLogLevel.Error, $"Test failed: {TestName}. Error: {ex.Message}");

                throw;
            }
        }

        /// <summary>
        /// Setup method to be implemented by derived classes.
        /// Called after base setup is complete.
        /// </summary>
        public abstract void SetUp();

        /// <summary>
        /// Test execution method to be implemented by derived classes.
        /// Contains the actual test logic.
        /// </summary>
        public abstract void ExecuteTest();

        /// <summary>
        /// Teardown method to be implemented by derived classes.
        /// Called before base teardown begins.
        /// </summary>
        public abstract void TearDown();

        /// <summary>
        /// Configures the WebDriver with standard settings.
        /// </summary>
        private void ConfigureDriver(IWebDriver driver)
        {
            if (driver == null) return;

            try
            {
                // Set timeouts
                var timeouts = driver.Manage().Timeouts();
                timeouts.ImplicitWait = TimeSpan.FromSeconds(Config.ImplicitWaitSeconds);
                timeouts.PageLoad = TimeSpan.FromSeconds(Config.PageLoadTimeoutSeconds);
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(Config.ScriptTimeoutSeconds);

                // Maximize window if configured
                if (Config.WindowMaximize)
                {
                    driver.Manage().Window.Maximize();
                }

                Logger.Debug("WebDriver configured with timeouts: Implicit={Implicit}s, PageLoad={PageLoad}s, Script={Script}s",
                    Config.ImplicitWaitSeconds, Config.PageLoadTimeoutSeconds, Config.ScriptTimeoutSeconds);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to configure WebDriver: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Takes a screenshot and saves it to the configured screenshot path.
        /// </summary>
        /// <param name="screenshotName">The name for the screenshot file.</param>
        /// <returns>The path to the saved screenshot.</returns>
        protected string TakeScreenshot(string screenshotName)
        {
            if (Driver == null)
            {
                throw new InvalidOperationException("WebDriver is not initialized.");
            }

            try
            {
                var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                var fileName = $"{screenshotName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filePath = System.IO.Path.Combine(Config.ScreenshotPath, fileName);

                // Ensure directory exists
                var directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                screenshot.SaveAsFile(filePath);
                Logger.Information("Screenshot saved: {FilePath}", filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to take screenshot: {ScreenshotName}", screenshotName);
                throw;
            }
        }

        /// <summary>
        /// Gets test data from the data repository.
        /// </summary>
        /// <param name="testCaseId">The test case ID.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="source">Optional data source name.</param>
        /// <returns>The test data value or null if not found.</returns>
        protected object? GetTestData(string testCaseId, string fieldName, string? source = null)
        {
            try
            {
                var testData = DataRepository?.GetTestData(testCaseId, source);
                if (testData != null && testData.TryGetValue(fieldName, out var value))
                {
                    return value;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to get test data: TestCaseId={TestCaseId}, Field={Field}, Error={Error}", testCaseId, fieldName, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets all test data for a test case ID.
        /// </summary>
        /// <param name="testCaseId">The test case ID.</param>
        /// <param name="source">Optional data source name.</param>
        /// <returns>Dictionary of test data or null if not found.</returns>
        protected Dictionary<string, object>? GetAllTestData(string testCaseId, string? source = null)
        {
            try
            {
                return DataRepository?.GetTestData(testCaseId, source);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to get all test data: TestCaseId={TestCaseId}, Error={Error}", testCaseId, ex.Message);
                return null;
            }
        }
    }
}

using OpenQA.Selenium;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace QualiFrame.Framework.Core.Services
{
    /// <summary>
    /// Service for test execution orchestration with retry, logging, and analytics.
    /// </summary>
    public class TestExecutionService : ITestExecutionService
    {
        private readonly TestMetrics _testMetrics;
        private readonly int _defaultMaxRetries;

        public TestExecutionService(TestMetrics testMetrics, int defaultMaxRetries = 3)
        {
            _testMetrics = testMetrics ?? throw new ArgumentNullException(nameof(testMetrics));
            _defaultMaxRetries = defaultMaxRetries;
        }

        /// <summary>
        /// Executes a test scenario with the provided driver.
        /// </summary>
        public async Task<bool> ExecuteTestAsync(IWebDriver driver, string scenarioName, Func<IWebDriver, Task<bool>> testAction)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (string.IsNullOrEmpty(scenarioName))
                throw new ArgumentException("Scenario name cannot be null or empty", nameof(scenarioName));
            if (testAction == null)
                throw new ArgumentNullException(nameof(testAction));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Logger.Information("Starting test execution: {ScenarioName}", scenarioName);
                var result = await testAction(driver);
                stopwatch.Stop();

                var status = result ? "Passed" : "Failed";
                _testMetrics.RecordExecution(scenarioName, status, stopwatch.Elapsed, null);

                if (result)
                {
                    Logger.Information("Test execution completed successfully: {ScenarioName} in {Duration}ms", 
                        scenarioName, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    Logger.Warning("Test execution failed: {ScenarioName} in {Duration}ms", 
                        scenarioName, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _testMetrics.RecordExecution(scenarioName, "Error", stopwatch.Elapsed, ex.Message);
                Logger.Error(ex, "Test execution error: {ScenarioName} in {Duration}ms", 
                    scenarioName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Executes a test scenario synchronously.
        /// </summary>
        public bool ExecuteTest(IWebDriver driver, string scenarioName, Func<IWebDriver, bool> testAction)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (string.IsNullOrEmpty(scenarioName))
                throw new ArgumentException("Scenario name cannot be null or empty", nameof(scenarioName));
            if (testAction == null)
                throw new ArgumentNullException(nameof(testAction));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Logger.Information("Starting test execution: {ScenarioName}", scenarioName);
                var result = testAction(driver);
                stopwatch.Stop();

                var status = result ? "Passed" : "Failed";
                _testMetrics.RecordExecution(scenarioName, status, stopwatch.Elapsed, null);

                if (result)
                {
                    Logger.Information("Test execution completed successfully: {ScenarioName} in {Duration}ms", 
                        scenarioName, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    Logger.Warning("Test execution failed: {ScenarioName} in {Duration}ms", 
                        scenarioName, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _testMetrics.RecordExecution(scenarioName, "Error", stopwatch.Elapsed, ex.Message);
                Logger.Error(ex, "Test execution error: {ScenarioName} in {Duration}ms", 
                    scenarioName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Retries a test action with configurable retry policy.
        /// </summary>
        public async Task<bool> RetryTestAsync(Func<Task<bool>> testAction, int maxRetries = 3)
        {
            if (testAction == null)
                throw new ArgumentNullException(nameof(testAction));

            int attempt = 0;
            Exception? lastException = null;

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    Logger.Debug("Test retry attempt {Attempt} of {MaxRetries}", attempt, maxRetries);
                    var result = await testAction();
                    
                    if (result)
                    {
                        Logger.Information("Test passed on attempt {Attempt}", attempt);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Logger.Warning("Test failed on attempt {Attempt} of {MaxRetries}: {Error}", 
                        attempt, maxRetries, ex.Message);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000 * attempt);
                    }
                }
            }

            if (lastException != null)
            {
                Logger.Error("Test failed after {MaxRetries} attempts: {Error}", maxRetries, lastException.Message);
                throw lastException;
            }

            return false;
        }
    }
}

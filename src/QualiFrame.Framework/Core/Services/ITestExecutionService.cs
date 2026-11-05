using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace QualiFrame.Framework.Core.Services
{
    /// <summary>
    /// Service interface for test execution orchestration.
    /// </summary>
    public interface ITestExecutionService
    {
        /// <summary>
        /// Executes a test scenario with the provided driver.
        /// </summary>
        Task<bool> ExecuteTestAsync(IWebDriver driver, string scenarioName, Func<IWebDriver, Task<bool>> testAction);

        /// <summary>
        /// Executes a test scenario synchronously.
        /// </summary>
        bool ExecuteTest(IWebDriver driver, string scenarioName, Func<IWebDriver, bool> testAction);

        /// <summary>
        /// Retries a test action with configurable retry policy.
        /// </summary>
        Task<bool> RetryTestAsync(Func<Task<bool>> testAction, int maxRetries = 3);
    }
}

using System;
using OpenQA.Selenium;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;
using WebDriverException = QualiFrame.Framework.Core.Exceptions.WebDriverException;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Checks the health of WebDriver instances.
    /// </summary>
    public static class DriverHealthChecker
    {
        /// <summary>
        /// Checks if the driver is healthy and responsive.
        /// </summary>
        public static bool IsHealthy(IWebDriver? driver)
        {
            if (driver == null)
            {
                Logger.Warning("Driver is null");
                return false;
            }

            try
            {
                // Try to get window handles - this will fail if driver is dead
                _ = driver.WindowHandles;
                
                // Try to get current URL - this will fail if driver is dead
                //_ = driver.Url;
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning("Driver health check failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validates driver health and throws exception if unhealthy.
        /// </summary>
        public static void ValidateHealth(IWebDriver? driver, string? browser = null)
        {
            if (!IsHealthy(driver))
            {
                throw new WebDriverException(
                    "WebDriver is not healthy or has been closed",
                    browser: browser,
                    testId: Logger.TestId
                );
            }
        }
    }
}

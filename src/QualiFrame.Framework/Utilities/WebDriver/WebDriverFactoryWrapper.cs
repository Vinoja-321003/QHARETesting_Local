using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using QualiFrame.Framework.Core.Interfaces;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Wrapper class that implements IWebDriverFactory interface for dependency injection.
    /// Delegates to EnhancedWebDriverFactory static methods.
    /// </summary>
    public class WebDriverFactoryWrapper : IWebDriverFactory
    {
        /// <summary>
        /// Gets the current WebDriver instance.
        /// </summary>
        public IWebDriver GetDriver()
        {
            return EnhancedWebDriverFactory.GetDriver();
        }

        /// <summary>
        /// Creates a new WebDriver instance.
        /// </summary>
        public IWebDriver CreateDriver()
        {
            return EnhancedWebDriverFactory.CreateDriver();
        }

        /// <summary>
        /// Gets a WebDriverWait instance.
        /// </summary>
        public WebDriverWait GetWebDriverWait(IWebDriver driver, int timeoutSeconds = 10)
        {
            return EnhancedWebDriverFactory.GetWebDriverWait(driver, timeoutSeconds);
        }

        /// <summary>
        /// Takes a screenshot and saves it.
        /// </summary>
        public string? TakeScreenshot(IWebDriver driver, string fileName)
        {
            return EnhancedWebDriverFactory.TakeScreenshot(driver, fileName);
        }

        /// <summary>
        /// Quits the current WebDriver instance.
        /// </summary>
        public void QuitDriver()
        {
            EnhancedWebDriverFactory.QuitDriver();
        }

        /// <summary>
        /// Closes the current WebDriver instance.
        /// </summary>
        public void CloseDriver()
        {
            EnhancedWebDriverFactory.CloseDriver();
        }
    }
}

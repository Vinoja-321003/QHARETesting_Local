using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace QualiFrame.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for WebDriver factory operations.
    /// </summary>
    public interface IWebDriverFactory
    {
        /// <summary>
        /// Gets the current WebDriver instance.
        /// </summary>
        IWebDriver GetDriver();

        /// <summary>
        /// Creates a new WebDriver instance.
        /// </summary>
        IWebDriver CreateDriver();

        /// <summary>
        /// Gets a WebDriverWait instance.
        /// </summary>
        WebDriverWait GetWebDriverWait(IWebDriver driver, int timeoutSeconds = 10);

        /// <summary>
        /// Takes a screenshot and saves it.
        /// Returns the path to the saved screenshot file, or null if failed.
        /// </summary>
        string? TakeScreenshot(IWebDriver driver, string fileName);

        /// <summary>
        /// Quits the current WebDriver instance.
        /// </summary>
        void QuitDriver();

        /// <summary>
        /// Closes the current WebDriver instance.
        /// </summary>
        void CloseDriver();
    }
}

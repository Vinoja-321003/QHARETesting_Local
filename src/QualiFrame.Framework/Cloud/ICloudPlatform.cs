using System.Collections.Generic;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// Interface for cloud testing platforms.
    /// </summary>
    public interface ICloudPlatform
    {
        /// <summary>
        /// Gets the platform name.
        /// </summary>
        string PlatformName { get; }

        /// <summary>
        /// Creates a WebDriver instance for the cloud platform.
        /// </summary>
        /// <param name="browser">Browser name.</param>
        /// <param name="options">Additional options.</param>
        /// <returns>WebDriver instance.</returns>
        IWebDriver CreateWebDriver(string browser, Dictionary<string, object>? options = null);

        /// <summary>
        /// Validates platform configuration.
        /// </summary>
        bool ValidateConfiguration();
    }
}

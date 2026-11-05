using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// Base class for cloud platform implementations.
    /// </summary>
    public abstract class BaseCloudPlatform : ICloudPlatform
    {
        protected readonly string Username;
        protected readonly string AccessKey;
        protected readonly Uri HubUrl;

        /// <summary>
        /// Gets the platform name.
        /// </summary>
        public abstract string PlatformName { get; }

        protected BaseCloudPlatform(string username, string accessKey, string hubUrl)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            AccessKey = accessKey ?? throw new ArgumentNullException(nameof(accessKey));
            HubUrl = new Uri(hubUrl);
        }

        /// <summary>
        /// Creates a WebDriver instance for the cloud platform.
        /// </summary>
        public virtual IWebDriver CreateWebDriver(string browser, Dictionary<string, object>? options = null)
        {
            var driverOptions = CreateDriverOptions(browser, options);
            var capabilities = ApplyPlatformOptions(driverOptions, options);
            
            Logger.Information("Creating {Platform} WebDriver for browser: {Browser}", PlatformName, browser);
            return new RemoteWebDriver(HubUrl, capabilities);
        }

        /// <summary>
        /// Validates platform configuration.
        /// </summary>
        public virtual bool ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                Logger.Warning("{Platform} configuration validation failed: Username is required", PlatformName);
                return false;
            }

            if (string.IsNullOrWhiteSpace(AccessKey))
            {
                Logger.Warning("{Platform} configuration validation failed: AccessKey is required", PlatformName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates driver options for the specified browser.
        /// </summary>
        protected virtual DriverOptions CreateDriverOptions(string browser, Dictionary<string, object>? options)
        {
            return browser.ToLower() switch
            {
                "chrome" => new OpenQA.Selenium.Chrome.ChromeOptions { AcceptInsecureCertificates = true },
                "firefox" => new OpenQA.Selenium.Firefox.FirefoxOptions { AcceptInsecureCertificates = true },
                "edge" => new OpenQA.Selenium.Edge.EdgeOptions { AcceptInsecureCertificates = true },
                "safari" => new OpenQA.Selenium.Safari.SafariOptions(),
                _ => throw new ArgumentException($"Browser '{browser}' is not supported for {PlatformName}")
            };
        }

        /// <summary>
        /// Applies platform-specific options to driver capabilities.
        /// </summary>
        protected abstract ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options);
    }
}

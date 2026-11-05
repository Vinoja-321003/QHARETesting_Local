using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// SauceLabs cloud platform implementation.
    /// </summary>
    public class SauceLabsPlatform : BaseCloudPlatform
    {
        private readonly string _region;

        public override string PlatformName => "SauceLabs";

        public SauceLabsPlatform(string username, string accessKey, string region = "us-west-1") 
            : base(username, accessKey, $"https://{username}:{accessKey}@ondemand.{region}.saucelabs.com:443/wd/hub")
        {
            _region = region;
        }

        protected override ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options)
        {
            driverOptions.BrowserVersion = options?.TryGetValue("BrowserVersion", out var version) == true 
                ? version.ToString() 
                : "latest";
            driverOptions.PlatformName = options?.TryGetValue("Platform", out var platform) == true 
                ? platform.ToString() 
                : "Windows 11";

            var sauceOptions = new Dictionary<string, object>
            {
                { "name", GetTestName() },
                { "build", GetBuildName() }
            };

            if (options?.TryGetValue("SeleniumVersion", out var seleniumVersion) == true && seleniumVersion != null)
            {
                sauceOptions["seleniumVersion"] = seleniumVersion.ToString() ?? string.Empty;
            }

            if (options?.TryGetValue("ScreenResolution", out var resolution) == true && resolution != null)
            {
                sauceOptions["screenResolution"] = resolution.ToString() ?? string.Empty;
            }

            driverOptions.AddAdditionalOption("sauce:options", sauceOptions);
            return driverOptions.ToCapabilities();
        }

        private string GetBuildName()
        {
            return Environment.GetEnvironmentVariable("SAUCE_BUILD_NAME") 
                   ?? $"QualiFrame Build {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }

        private string GetTestName()
        {
            return Environment.GetEnvironmentVariable("SAUCE_TEST_NAME") 
                   ?? NUnit.Framework.TestContext.CurrentContext?.Test?.Name 
                   ?? "QualiFrame Test";
        }
    }
}

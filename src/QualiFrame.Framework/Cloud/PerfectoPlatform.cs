using System;
using System.Collections.Generic;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// Perfecto cloud platform implementation.
    /// </summary>
    public class PerfectoPlatform : BaseCloudPlatform
    {
        private readonly string _cloudName;

        public override string PlatformName => "Perfecto";

        public PerfectoPlatform(string username, string accessKey, string cloudName) 
            : base(username, accessKey, $"https://{cloudName}.perfectomobile.com/nexperience/perfectomobile/wd/hub")
        {
            _cloudName = cloudName ?? throw new ArgumentNullException(nameof(cloudName));
        }

        protected override ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options)
        {
            driverOptions.BrowserVersion = options?.TryGetValue("BrowserVersion", out var version) == true 
                ? version.ToString() 
                : "latest";

            var perfectoOptions = new Dictionary<string, object>
            {
                { "user", Username },
                { "password", AccessKey },
                { "platformName", options?.TryGetValue("Platform", out var platform) == true && platform != null
                    ? platform.ToString() ?? "Windows"
                    : "Windows" },
                { "platformVersion", options?.TryGetValue("PlatformVersion", out var platformVersion) == true && platformVersion != null
                    ? platformVersion.ToString() ?? "10"
                    : "10" },
                { "browserName", driverOptions.BrowserName ?? "Chrome" },
                { "browserVersion", driverOptions.BrowserVersion ?? "latest" }
            };

            if (options?.TryGetValue("TestName", out var testName) == true && testName != null)
            {
                perfectoOptions["testName"] = testName.ToString() ?? string.Empty;
            }

            if (options?.TryGetValue("Tags", out var tags) == true && tags != null)
            {
                perfectoOptions["tags"] = tags;
            }

            if (options?.TryGetValue("TakeScreenshot", out var takeScreenshot) == true && takeScreenshot != null)
            {
                perfectoOptions["takeScreenshot"] = takeScreenshot.ToString()?.ToLower() ?? "false";
            }

            driverOptions.AddAdditionalOption("perfecto:options", perfectoOptions);
            return driverOptions.ToCapabilities();
        }
    }
}

using System;
using System.Collections.Generic;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// Experitest cloud platform implementation.
    /// </summary>
    public class ExperitestPlatform : BaseCloudPlatform
    {
        public override string PlatformName => "Experitest";

        public ExperitestPlatform(string username, string accessKey) 
            : base(username, accessKey, $"https://{username}:{accessKey}@cloud.experitest.com/wd/hub")
        {
        }

        protected override ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options)
        {
            driverOptions.BrowserVersion = options?.TryGetValue("BrowserVersion", out var version) == true 
                ? version.ToString() 
                : "latest";

            var experitestOptions = new Dictionary<string, object>
            {
                { "user", Username },
                { "password", AccessKey },
                { "platformName", options?.TryGetValue("Platform", out var platform) == true && platform != null
                    ? platform.ToString() ?? "Windows"
                    : "Windows" },
                { "browserName", driverOptions.BrowserName ?? "Chrome" },
                { "browserVersion", driverOptions.BrowserVersion ?? "latest" }
            };

            if (options?.TryGetValue("TestName", out var testName) == true && testName != null)
            {
                experitestOptions["testName"] = testName.ToString() ?? string.Empty;
            }

            if (options?.TryGetValue("ProjectName", out var projectName) == true && projectName != null)
            {
                experitestOptions["projectName"] = projectName.ToString() ?? string.Empty;
            }

            driverOptions.AddAdditionalOption("experitest:options", experitestOptions);
            return driverOptions.ToCapabilities();
        }
    }
}

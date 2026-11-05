using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// LambdaTest cloud platform implementation.
    /// </summary>
    public class LambdaTestPlatform : BaseCloudPlatform
    {
        public override string PlatformName => "LambdaTest";

        public LambdaTestPlatform(string username, string accessKey) 
            : base(username, accessKey, $"https://{username}:{accessKey}@hub.lambdatest.com/wd/hub")
        {
        }

        protected override ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options)
        {
            driverOptions.BrowserVersion = options?.TryGetValue("BrowserVersion", out var version) == true 
                ? version.ToString() 
                : "latest";

            var ltOptions = new Dictionary<string, object>
            {
                { "username", Username },
                { "accessKey", AccessKey },
                { "build", GetBuildName() },
                { "name", GetTestName() },
                { "platformName", options?.TryGetValue("Platform", out var platform) == true && platform != null
                    ? platform.ToString() ?? "Windows 11"
                    : "Windows 11" },
                { "resolution", options?.TryGetValue("Resolution", out var resolution) == true && resolution != null
                    ? resolution.ToString() ?? "1920x1080"
                    : "1920x1080" },
                { "video", options?.TryGetValue("Video", out var video) == true && video != null
                    ? video.ToString()?.ToLower() ?? "true"
                    : "true" },
                { "network", options?.TryGetValue("Network", out var network) == true && network != null
                    ? network.ToString()?.ToLower() ?? "true"
                    : "true" },
                { "console", options?.TryGetValue("Console", out var console) == true && console != null
                    ? console.ToString()?.ToLower() ?? "true"
                    : "true" }
            };

            if (options?.TryGetValue("Tunnel", out var tunnel) == true && tunnel?.ToString()?.ToLower() == "true")
            {
                ltOptions["tunnel"] = true;
            }

            driverOptions.AddAdditionalOption("LT:Options", ltOptions);
            return driverOptions.ToCapabilities();
        }

        private string GetBuildName()
        {
            return Environment.GetEnvironmentVariable("LT_BUILD_NAME") 
                   ?? $"QualiFrame Build {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }

        private string GetTestName()
        {
            return Environment.GetEnvironmentVariable("LT_TEST_NAME") 
                   ?? NUnit.Framework.TestContext.CurrentContext?.Test?.Name 
                   ?? "QualiFrame Test";
        }
    }
}

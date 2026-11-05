using System;
using System.Collections.Generic;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Cloud
{
    /// <summary>
    /// BrowserStack cloud platform implementation.
    /// </summary>
    public class BrowserStackPlatform : BaseCloudPlatform
    {
        public override string PlatformName => "BrowserStack";

        public BrowserStackPlatform(string username, string accessKey) 
            : base(username, accessKey, $"https://{username}:{accessKey}@hub.browserstack.com/wd/hub")
        {
        }

        protected override ICapabilities ApplyPlatformOptions(DriverOptions driverOptions, Dictionary<string, object>? options)
        {
            driverOptions.BrowserVersion = options?.TryGetValue("BrowserVersion", out var version) == true 
                ? version.ToString() 
                : "latest";

            var bstackOptions = new Dictionary<string, object>
            {
                { "os", options?.TryGetValue("OS", out var os) == true && os != null ? os.ToString() ?? "Windows" : "Windows" },
                { "osVersion", options?.TryGetValue("OSVersion", out var osVersion) == true && osVersion != null ? osVersion.ToString() ?? "11" : "11" },
                { "buildName", GetBuildName() },
                { "sessionName", GetTestName() }
            };

            if (options?.TryGetValue("Resolution", out var resolution) == true && resolution != null)
            {
                bstackOptions["resolution"] = resolution.ToString() ?? string.Empty;
            }

            if (options?.TryGetValue("Local", out var local) == true && local != null && local.ToString()?.ToLower() == "true")
            {
                bstackOptions["local"] = true;
            }

            driverOptions.AddAdditionalOption("bstack:options", bstackOptions);
            return driverOptions.ToCapabilities();
        }

        private string GetBuildName()
        {
            return Environment.GetEnvironmentVariable("BROWSERSTACK_BUILD_NAME") 
                   ?? $"QualiFrame Build {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }

        private string GetTestName()
        {
            return Environment.GetEnvironmentVariable("BROWSERSTACK_TEST_NAME") 
                   ?? NUnit.Framework.TestContext.CurrentContext?.Test?.Name 
                   ?? "QualiFrame Test";
        }
    }
}

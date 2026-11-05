using System;
using System.Threading;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.Mobile
{
    /// <summary>
    /// Appium driver factory mirroring EnhancedWebDriverFactory lifecycle.
    /// Use this instead of constructing AndroidDriver/IOSDriver in steps or tests.
    /// </summary>
    public static class AppiumDriverFactory
    {
        private static readonly AsyncLocal<AppiumDriver?> CurrentDriver = new();
        private static readonly TestConfiguration Config = TestConfiguration.Instance;

        public static void SetCurrentDriver(AppiumDriver driver)
        {
            CurrentDriver.Value = driver;
        }

        public static AppiumDriver? TryGetDriver() => CurrentDriver.Value;

        public static AppiumDriver GetDriver()
        {
            return CurrentDriver.Value ?? CreateDriver();
        }

        public static AppiumOptions CreateOptions()
        {
            var options = new AppiumOptions();
            var platform = (Config.PlatformName ?? "Android").Trim();

            if (platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
            {
                options.PlatformName = MobilePlatform.IOS;
                options.DeviceName = string.IsNullOrWhiteSpace(Config.DeviceName) ? "iOS Device" : Config.DeviceName;
                options.AutomationName = string.IsNullOrWhiteSpace(Config.AutomationName) ? "XCUITest" : Config.AutomationName;
            }
            else
            {
                options.PlatformName = MobilePlatform.Android;
                options.DeviceName = string.IsNullOrWhiteSpace(Config.DeviceName) ? "Android Device" : Config.DeviceName;
                options.AutomationName = string.IsNullOrWhiteSpace(Config.AutomationName) ? "UiAutomator2" : Config.AutomationName;
            }

            if (!string.IsNullOrWhiteSpace(Config.AppPackage))
            {
                options.App = Config.AppPackage;
            }

            if (!string.IsNullOrWhiteSpace(Config.MobileBrowserName))
            {
                options.BrowserName = Config.MobileBrowserName;
            }

            options.AddAdditionalAppiumOption("noReset", Config.MobileNoReset);
            if (Config.MobileAutoWebView)
            {
                options.AddAdditionalAppiumOption("autoWebview", true);
            }

            return options;
        }

        public static AppiumDriver CreateDriver()
        {
            AppiumServerManager.StartAppiumServerIfNeeded();

            var serverUrl = Config.AppiumServerUrl;
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                throw new InvalidOperationException("Mobile:AppiumServerUrl is not configured.");
            }

            if (!AppiumServerManager.IsAppiumServerRunning(serverUrl))
            {
                throw new InvalidOperationException($"Appium server is not reachable at {serverUrl}.");
            }

            var options = CreateOptions();
            var platform = Config.PlatformName ?? "Android";
            AppiumDriver driver;

            if (platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
            {
                driver = new IOSDriver(new Uri(serverUrl), options);
                Logger.Information("Created iOS Appium driver for {Device}", Config.DeviceName);
            }
            else
            {
                driver = new AndroidDriver(new Uri(serverUrl), options);
                Logger.Information("Created Android Appium driver for {Device}", Config.DeviceName);
            }

            CurrentDriver.Value = driver;
            return driver;
        }

        public static void QuitDriver()
        {
            try
            {
                CurrentDriver.Value?.Quit();
            }
            catch (Exception ex)
            {
                Logger.Warning("Error quitting Appium driver: {Error}", ex.Message);
            }
            finally
            {
                CurrentDriver.Value = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium.Appium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Mobile.Utilities
{
    /// <summary>
    /// Switches an Appium session between NATIVE_APP and WEBVIEW contexts.
    /// </summary>
    public static class MobileContextSwitcher
    {
        public static IReadOnlyCollection<string> GetContexts(AppiumDriver driver)
        {
            return driver.Contexts;
        }

        public static string CurrentContext(AppiumDriver driver) => driver.Context;

        public static void SwitchToNative(AppiumDriver driver)
        {
            driver.Context = "NATIVE_APP";
            Logger.Information("Switched mobile context to NATIVE_APP");
        }

        public static void SwitchToWebView(AppiumDriver driver, string? contextName = null)
        {
            var configured = contextName;
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = TestConfiguration.Instance.MobileWebViewContext;
            }

            var contexts = driver.Contexts;
            var target = !string.IsNullOrWhiteSpace(configured)
                ? contexts.FirstOrDefault(c => string.Equals(c, configured, StringComparison.OrdinalIgnoreCase))
                : contexts.FirstOrDefault(c => c.Contains("WEBVIEW", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException(
                    $"No WEBVIEW context is available. Contexts: {string.Join(", ", contexts)}");
            }

            driver.Context = target;
            Logger.Information("Switched mobile context to {Context}", target);
        }

        public static bool HasWebView(AppiumDriver driver)
        {
            return driver.Contexts.Any(c => c.Contains("WEBVIEW", StringComparison.OrdinalIgnoreCase));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.Core.Interfaces;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Configuration
{
    /// <summary>
    /// Validates test configuration settings.
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Validates the test configuration.
        /// </summary>
        public static ValidationResult Validate(TestConfiguration config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(config.Browser))
            {
                errors.Add("Browser is not specified in configuration");
            }
            else
            {
                var validBrowsers = new[] { "chrome", "firefox", "edge", "safari" };
                if (!validBrowsers.Contains(config.Browser.ToLower()))
                {
                    errors.Add($"Invalid browser specified: {config.Browser}. Valid browsers are: {string.Join(", ", validBrowsers)}");
                }
            }

            if (string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                errors.Add("BaseUrl is not specified in configuration");
            }
            else if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out _))
            {
                errors.Add($"Invalid BaseUrl format: {config.BaseUrl}");
            }

            if (config.ImplicitWait < 0)
            {
                errors.Add("ImplicitWait cannot be negative");
            }
            if (config.PageLoadTimeout < 0)
            {
                errors.Add("PageLoadTimeout cannot be negative");
            }
            if (config.ImplicitWait > 60)
            {
                warnings.Add($"ImplicitWait is set to {config.ImplicitWait} seconds, which is unusually high");
            }

            var healer = SelfHealingProviderResolver.Resolve(config);
            if (healer == SelfHealingProvider.Qhare && string.IsNullOrWhiteSpace(config.QhareBaseUrl))
            {
                errors.Add("SelfHealing Provider is Qhare but Qhare:BaseUrl is not specified");
            }
            if (healer == SelfHealingProvider.Qhare &&
                !string.IsNullOrWhiteSpace(config.QhareBaseUrl) &&
                !Uri.TryCreate(config.QhareBaseUrl, UriKind.Absolute, out _))
            {
                errors.Add($"Invalid Qhare:BaseUrl format: {config.QhareBaseUrl}");
            }

            if (config.SauceLabsEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.SauceLabsUsername))
                {
                    errors.Add("SauceLabs is enabled but Username is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.SauceLabsAccessKey))
                {
                    errors.Add("SauceLabs is enabled but AccessKey is not specified");
                }
            }

            if (config.BrowserStackEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.BrowserStackUsername))
                {
                    errors.Add("BrowserStack is enabled but Username is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.BrowserStackAccessKey))
                {
                    errors.Add("BrowserStack is enabled but AccessKey is not specified");
                }
            }

            if (config.LambdaTestEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.LambdaTestUsername))
                {
                    errors.Add("LambdaTest is enabled but Username is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.LambdaTestAccessKey))
                {
                    errors.Add("LambdaTest is enabled but AccessKey is not specified");
                }
            }

            if (config.PerfectoEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.PerfectoUsername))
                {
                    errors.Add("Perfecto is enabled but Username is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.PerfectoAccessKey))
                {
                    errors.Add("Perfecto is enabled but AccessKey is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.PerfectoCloudName))
                {
                    errors.Add("Perfecto is enabled but CloudName is not specified");
                }
            }

            if (config.ExperitestEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.ExperitestUsername))
                {
                    errors.Add("Experitest is enabled but Username is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.ExperitestAccessKey))
                {
                    errors.Add("Experitest is enabled but AccessKey is not specified");
                }
            }

            if (config.AwsDeviceFarmEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.AwsDeviceFarmAccessKeyId))
                {
                    errors.Add("AWS Device Farm is enabled but AccessKeyId is not specified");
                }
                if (string.IsNullOrWhiteSpace(config.AwsDeviceFarmSecretAccessKey))
                {
                    errors.Add("AWS Device Farm is enabled but SecretAccessKey is not specified");
                }
            }

            if (!string.IsNullOrWhiteSpace(config.AppiumServerUrl))
            {
                if (!Uri.TryCreate(config.AppiumServerUrl, UriKind.Absolute, out _))
                {
                    errors.Add($"Invalid AppiumServerUrl format: {config.AppiumServerUrl}");
                }
            }

            if (config.ApiTimeout < 0)
            {
                errors.Add("ApiTimeout cannot be negative");
            }
            if (config.ApiRetryCount < 0)
            {
                errors.Add("ApiRetryCount cannot be negative");
            }

            if (string.IsNullOrWhiteSpace(config.LogPath))
            {
                warnings.Add("LogPath is not specified, using default path");
            }

            var validLogLevels = new[] { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };
            if (!validLogLevels.Contains(config.LogLevel, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Invalid LogLevel: {config.LogLevel}. Valid levels are: {string.Join(", ", validLogLevels)}");
            }

            if (config.MaxParallelTests < 1)
            {
                errors.Add("MaxParallelTests must be at least 1");
            }
            if (config.MaxParallelTests > 10)
            {
                warnings.Add($"MaxParallelTests is set to {config.MaxParallelTests}, which may cause resource contention");
            }

            var result = new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };

            if (!result.IsValid)
            {
                Logger.Error("Configuration validation failed with {ErrorCount} error(s)", errors.Count);
                foreach (var error in errors)
                {
                    Logger.Error("Configuration error: {Error}", error);
                }
            }

            if (warnings.Count > 0)
            {
                Logger.Warning("Configuration validation found {WarningCount} warning(s)", warnings.Count);
                foreach (var warning in warnings)
                {
                    Logger.Warning("Configuration warning: {Warning}", warning);
                }
            }

            if (result.IsValid && warnings.Count == 0)
            {
                Logger.Information("Configuration validation passed successfully");
            }

            return result;
        }
    }

    /// <summary>
    /// Result of configuration validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}

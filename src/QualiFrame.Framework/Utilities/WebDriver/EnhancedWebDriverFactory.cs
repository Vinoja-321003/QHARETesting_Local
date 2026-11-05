using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;
using OpenQA.Selenium.Support.UI;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Cloud;
using QualiFrame.Framework.Utilities.Logging;
using Reqnroll;
using Polly;
using WebDriverException = QualiFrame.Framework.Core.Exceptions.WebDriverException;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Enhanced WebDriver factory with WebDriverManager, retry mechanism, and health checks.
    /// Note: This class uses static methods for backward compatibility. For dependency injection,
    /// use WebDriverFactoryWrapper which implements IWebDriverFactory.
    /// </summary>
    public class EnhancedWebDriverFactory
    {
        private static readonly System.Threading.AsyncLocal<IWebDriver?> _currentDriver = new System.Threading.AsyncLocal<IWebDriver?>();
        private static readonly TestConfiguration _config = TestConfiguration.Instance;
        private const int MaxRetryAttempts = 3;
        private const int RetryDelaySeconds = 2;

        /// <summary>
        /// Sets the current driver instance. Used to ensure driver created in hooks is properly stored.
        /// </summary>
        public static void SetCurrentDriver(IWebDriver driver)
        {
            _currentDriver.Value = driver;
        }

        /// <summary>
        /// Gets the current driver instance.
        /// </summary>
        public static IWebDriver GetDriver()
        {
            // Return existing driver if available and healthy
            // Driver should be created in BeforeScenario hook to avoid multiple instances
            if (_currentDriver.Value == null || !DriverHealthChecker.IsHealthy(_currentDriver.Value))
            {
                Logger.Warning("GetDriver() called but no healthy driver exists. Creating new driver (this may indicate a driver lifecycle issue).");
                _currentDriver.Value = CreateDriver();
            }
            return _currentDriver.Value!;
        }

        /// <summary>
        /// Creates a new WebDriver instance with retry mechanism.
        /// </summary>
        public static IWebDriver CreateDriver()
        {
            var browser = _config.Browser.ToLower();
            var headless = _config.Headless;
            var remoteUrl = _config.RemoteWebDriverUrl;

            Logger.Information("Creating WebDriver for browser: {Browser}, Headless: {Headless}", browser, headless);

            // Retry policy for driver creation
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(RetryDelaySeconds * retryAttempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Logger.Warning(
                            "Driver creation attempt {RetryCount} failed: {Message}. Retrying in {Delay} seconds...",
                            retryCount, exception.Message, timeSpan.TotalSeconds);
                    });

            var driver = retryPolicy.Execute(() =>
            {
                // Cloud providers take precedence if enabled (check in priority order)
                var cloudPlatform = CloudPlatformFactory.GetActivePlatform(_config);
                if (cloudPlatform != null)
                {
                    if (!cloudPlatform.ValidateConfiguration())
                    {
                        throw new WebDriverException(
                            $"{cloudPlatform.PlatformName} configuration is invalid. Please check credentials.",
                            browser: browser,
                            testId: Logger.TestId);
                    }

                    var options = new Dictionary<string, object>();
                    if (headless && browser.ToLower() == "chrome")
                    {
                        options["Headless"] = true;
                    }

                    // Opt-in only. Do not pass Video=false — some clouds default video on when the key is omitted.
                    if (_config.VideoRecording)
                    {
                        options["Video"] = true;
                    }

                    return cloudPlatform.CreateWebDriver(browser, options);
                }

                // Fallback to local drivers
                return browser switch
                {
                    "chrome" => CreateChromeDriver(headless, remoteUrl),
                    "firefox" => CreateFirefoxDriver(headless, remoteUrl),
                    "edge" => CreateEdgeDriver(headless, remoteUrl),
                    "safari" => CreateSafariDriver(remoteUrl),
                    _ => throw new WebDriverException(
                        $"Browser '{browser}' is not supported",
                        browser: browser,
                        testId: Logger.TestId)
                };
            });

            var healer = SelfHealingProviderResolver.Resolve(_config);
            if (healer == SelfHealingProvider.Qhare)
            {
                Logger.Information("Wrapping WebDriver with QHARE self-healing wrapper");
                driver = new QhareDriverWrapper(driver, _config.GetQhareConfiguration());
            }
            else if (healer == SelfHealingProvider.Healenium)
            {
                var healeniumConfig = _config.GetHealeniumConfiguration();
                Logger.Information("Wrapping WebDriver with Healenium self-healing wrapper");
                driver = new HealeniumDriverWrapper(driver, healeniumConfig);
            }

            // Opt-in screenshot sequence; default VideoRecording=false is a no-op.
            VideoRecordingManager.TryStart(driver);

            return driver;
        }

        private static IWebDriver CreateChromeDriver(bool headless, string? remoteUrl)
        {
            var options = new ChromeOptions();

            if (headless)
            {
                options.AddArgument("--headless=new");
            }

            // Standard Chrome arguments for automation
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddExcludedArgument("enable-logging");
            options.AddAdditionalOption("useAutomationExtension", false);

            // SSL/Certificate handling
            options.AcceptInsecureCertificates = true;
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--ignore-ssl-errors");
            options.AddArgument("--ignore-certificate-errors-spki-list");

            // Disable Chrome password manager
            options.AddUserProfilePreference("credentials_enable_service", false);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);
            options.AddUserProfilePreference("profile.password_manager_leak_detection", false);

            if (!string.IsNullOrEmpty(remoteUrl))
            {
                Logger.Information("Creating remote ChromeDriver: {Url}", remoteUrl);
                return new RemoteWebDriver(new Uri(remoteUrl), options);
            }

            Logger.Information("Creating local ChromeDriver");
            var driver = new ChromeDriver(options);
            ConfigureDriver(driver);
            return driver;
        }

        private static IWebDriver CreateFirefoxDriver(bool headless, string? remoteUrl)
        {
            var options = new FirefoxOptions();

            if (headless)
            {
                options.AddArgument("--headless");
            }

            if (!string.IsNullOrEmpty(remoteUrl))
            {
                Logger.Information("Creating remote FirefoxDriver: {Url}", remoteUrl);
                return new RemoteWebDriver(new Uri(remoteUrl), options);
            }

            Logger.Information("Creating local FirefoxDriver");
            var driver = new FirefoxDriver(options);
            ConfigureDriver(driver);
            return driver;
        }

        private static IWebDriver CreateEdgeDriver(bool headless, string? remoteUrl)
        {
            var options = new EdgeOptions();

            if (headless)
            {
                options.AddArgument("--headless=new");
            }

            // Standard Edge arguments for automation
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddExcludedArgument("enable-logging");
            options.AddAdditionalOption("useAutomationExtension", false);

            // SSL/Certificate handling
            options.AcceptInsecureCertificates = true;
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--ignore-ssl-errors");
            options.AddArgument("--ignore-certificate-errors-spki-list");

            if (!string.IsNullOrEmpty(remoteUrl))
            {
                Logger.Information("Creating remote EdgeDriver: {Url}", remoteUrl);
                return new RemoteWebDriver(new Uri(remoteUrl), options);
            }

            Logger.Information("Creating local EdgeDriver");
            var driver = new EdgeDriver(options);
            ConfigureDriver(driver);
            return driver;
        }

        private static IWebDriver CreateSafariDriver(string? remoteUrl)
        {
            if (!string.IsNullOrEmpty(remoteUrl))
            {
                Logger.Information("Creating remote SafariDriver: {Url}", remoteUrl);
                return new RemoteWebDriver(new Uri(remoteUrl), new SafariOptions());
            }

            Logger.Information("Creating local SafariDriver");
            var driver = new SafariDriver();
            ConfigureDriver(driver);
            return driver;
        }

        /// <summary>
        /// Configures driver settings after creation.
        /// </summary>
        private static void ConfigureDriver(IWebDriver driver)
        {
            try
            {
                if (_config.WindowMaximize)
                {
                    driver.Manage().Window.Maximize();
                }

                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(_config.ImplicitWaitSeconds);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_config.PageLoadTimeoutSeconds);
                driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(_config.ScriptTimeoutSeconds);

                Logger.Information("Driver configured: ImplicitWait={ImplicitWait}s, PageLoadTimeout={PageLoadTimeout}s, ScriptTimeout={ScriptTimeout}s",
                    _config.ImplicitWaitSeconds, _config.PageLoadTimeoutSeconds, _config.ScriptTimeoutSeconds);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to configure driver: {Message}", ex.Message);
            }
        }

        public static WebDriverWait GetWebDriverWait(IWebDriver driver, int timeoutSeconds = 10)
        {
            return new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        }

        public static string? TakeScreenshot(IWebDriver driver, string fileName)
        {
            try
            {
                if (!DriverHealthChecker.IsHealthy(driver))
                {
                    Logger.Warning("Cannot take screenshot: driver is not healthy");
                    return null;
                }

                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                
                var screenshotDir = _config.ScreenshotPath;
                if (!Path.IsPathRooted(screenshotDir))
                {
                    screenshotDir = Path.Combine(Directory.GetCurrentDirectory(), screenshotDir);
                }
                screenshotDir = Path.GetFullPath(screenshotDir);
                
                var screenshotPath = Path.Combine(screenshotDir, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

                screenshot.SaveAsFile(screenshotPath);
                Logger.Information("Screenshot saved: {Path}", screenshotPath);
                return screenshotPath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to take screenshot: {FileName}", fileName);
                return null;
            }
        }

        /// <summary>
        /// Quits and disposes the current driver instance.
        /// </summary>
        public static void QuitDriver()
        {
            VideoRecordingManager.Stop();
            IWebDriver? driverToQuit = null;
            
            try
            {
                if (_currentDriver.Value != null)
                {
                    driverToQuit = _currentDriver.Value;
                    _currentDriver.Value = null;
                    
                    try
                    {
                        if (DriverHealthChecker.IsHealthy(driverToQuit))
                        {
                            driverToQuit.Quit();
                            Logger.Information("Driver quit successfully");
                        }
                        else
                        {
                            Logger.Warning("Driver was already closed or unhealthy, attempting dispose");
                            driverToQuit.Dispose();
                        }
                    }
                    catch (Exception quitEx)
                    {
                        Logger.Warning("Error during driver quit, attempting dispose: {Error}", quitEx.Message);
                        try
                        {
                            driverToQuit.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Warning("Error during driver dispose: {Error}", disposeEx.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error during driver cleanup: {Error}", ex.Message);
            }
            finally
            {
                _currentDriver.Value = null;
                
                // Force cleanup of orphaned processes
                try
                {
                    if (driverToQuit != null)
                    {
                        var driverType = driverToQuit.GetType().Name;
                        if (driverType == "ChromeDriver" || driverType == "EdgeDriver")
                        {
                            var processName = driverType == "ChromeDriver" ? "chrome" : "msedge";
                            var processes = Process.GetProcessesByName(processName)
                                .Where(p => 
                                {
                                    try
                                    {
                                        return string.IsNullOrEmpty(p.MainWindowTitle) || 
                                               p.MainWindowTitle.Contains("data:") ||
                                               p.MainWindowTitle.Contains("chrome://");
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                })
                                .ToList();
                            
                            if (processes.Any())
                            {
                                Logger.Debug("Cleaning up {Count} orphaned {Browser} processes", processes.Count, processName);
                                foreach (var process in processes)
                                {
                                    try
                                    {
                                        process.Kill();
                                        process.WaitForExit(1000);
                                    }
                                    catch
                                    {
                                        // Ignore individual process kill errors
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore process cleanup errors
                }
            }
        }

        public static void CloseDriver()
        {
            VideoRecordingManager.Stop();
            try
            {
                if (_currentDriver.Value != null && DriverHealthChecker.IsHealthy(_currentDriver.Value))
                {
                    _currentDriver.Value.Close();
                    Logger.Information("Driver closed successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error during driver close: {Error}", ex.Message);
            }
        }

        private static class TestContextInfo
        {
            public static string GetScenarioNameOrDefault()
            {
                try
                {
#pragma warning disable CS0618
                    return ScenarioContext.Current?.ScenarioInfo?.Title ?? "QualiFrame Scenario";
#pragma warning restore CS0618
                }
                catch { return "QualiFrame Scenario"; }
            }

            public static string GetBuildName()
            {
                var ciBuild = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER")
                    ?? Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER")
                    ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
                return $"QualiFrame-CSharp-{ciBuild}";
            }
        }
    }
}

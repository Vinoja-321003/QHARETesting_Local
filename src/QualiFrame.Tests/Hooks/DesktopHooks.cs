using Reqnroll;
using NUnit.Framework;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Reports;
using System;
using System.IO;
using System.Runtime.Versioning;
using Allure.Net.Commons;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Hooks
{
    /// <summary>
    /// Reqnroll hooks specific to desktop (FlaUI) automation scenarios.
    /// </summary>
    [Binding]
    [SupportedOSPlatform("windows")]
    public class DesktopHooks
    {
        [BeforeScenario("@Desktop")]
        public void BeforeDesktopScenario()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive($"Desktop automation scenarios require Windows/FlaUI. Current OS: {Environment.OSVersion.Platform}");
                return;
            }

            try
            {
                var session = DesktopSessionFactory.GetSession();
                ExtentReportManager.LogPass(
                    $"FlaUI desktop session started for '{session.Title}' (PID {session.ProcessId}).");
            }
            catch (Exception ex)
            {
                Assert.Inconclusive(
                    "Cannot start a FlaUI desktop session for Desktop tests. " +
                    $"Error: {ex.Message}");
            }
        }

        [AfterScenario("@Desktop")]
        public void AfterDesktopScenario(ScenarioContext scenarioContext)
        {
            var scenarioTitle = scenarioContext.ScenarioInfo.Title;

            try
            {
                if (scenarioContext.TestError != null && DesktopSessionFactory.HasSession)
                {
                    try
                    {
                        var session = DesktopSessionFactory.GetSession();
                        Logger.Information(
                            "Desktop diagnostic - Scenario '{Scenario}', WindowTitle='{Title}', ProcessId='{ProcessId}'",
                            scenarioTitle,
                            session.Title,
                            session.ProcessId);

                        var screenshotPath = CaptureDesktopScreenshot(session, scenarioTitle);

                        if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                        {
                            ExtentReportManager.AddScreenshot(screenshotPath, "Desktop Failure Screenshot");

                            try
                            {
                                var screenshotBytes = File.ReadAllBytes(screenshotPath);
                                AllureApi.AddAttachment("Desktop Failure Screenshot", "image/png", screenshotBytes);
                                Logger.Debug("Desktop failure screenshot added to Allure.");
                            }
                            catch (Exception allureEx)
                            {
                                Logger.Warning("Failed to add desktop screenshot to Allure: {Error}", allureEx.Message);
                            }
                        }
                    }
                    catch (Exception diagEx)
                    {
                        Logger.Warning("Failed to capture desktop diagnostics: {Error}", diagEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error during desktop AfterScenario diagnostics: {Error}", ex.Message);
            }
            finally
            {
                DesktopSessionFactory.Quit();
                ExtentReportManager.LogPass("Desktop application session closed successfully");
            }
        }

        private static string CaptureDesktopScreenshot(DesktopSession session, string scenarioTitle)
        {
            try
            {
                var config = TestConfiguration.Instance;
                var screenshotsRoot = config.ScreenshotsPath;
                if (!Path.IsPathRooted(screenshotsRoot))
                {
                    screenshotsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), screenshotsRoot));
                }

                Directory.CreateDirectory(screenshotsRoot);

                var safeTitle = string.Concat(scenarioTitle.Split(Path.GetInvalidFileNameChars()));
                if (safeTitle.Length > 80)
                {
                    safeTitle = safeTitle[..80];
                }

                var fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}_desktop.png";
                var fullPath = Path.Combine(screenshotsRoot, fileName);
                session.CaptureScreenshot(fullPath);
                Logger.Information("Desktop screenshot captured: {Path}", fullPath);
                return fullPath;
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to capture desktop screenshot: {Error}", ex.Message);
                return string.Empty;
            }
        }
    }
}

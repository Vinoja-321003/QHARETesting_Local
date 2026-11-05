using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Core reusable desktop actions built on top of DesktopElement and FlaUI.
    /// All interactions are retry-safe, logged, and Allure-friendly.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class DesktopActions
    {
        public static void Click(DesktopElement element, string? description = null)
        {
            ExecuteWithDiagnostics(element, "Click", description, () => element.Click());
        }

        public static void DoubleClick(DesktopElement element, string? description = null)
        {
            ExecuteWithDiagnostics(element, "DoubleClick", description, () =>
            {
                SmartDesktopActions.Retry(() => FindElementOnce(element).DoubleClick());
            });
        }

        public static void RightClick(DesktopElement element, string? description = null)
        {
            ExecuteWithDiagnostics(element, "RightClick", description, () =>
            {
                SmartDesktopActions.Retry(() => FindElementOnce(element).RightClick());
            });
        }

        public static void Hover(DesktopElement element, string? description = null)
        {
            ExecuteWithDiagnostics(element, "Hover", description, () =>
            {
                SmartDesktopActions.Retry(() =>
                {
                    var target = FindElementOnce(element);
                    Mouse.MoveTo(target.GetClickablePoint());
                });
            });
        }

        public static void SendKeys(DesktopElement element, string text, bool clearBefore = false, string? description = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            ExecuteWithDiagnostics(element, "SendKeys", description ?? $"SendKeys '{text}'", () =>
            {
                SmartDesktopActions.Retry(() => DesktopUiQuery.SetValue(FindElementOnce(element), text, clearBefore));
            });
        }

        public static void Clear(DesktopElement element, string? description = null)
        {
            ExecuteWithDiagnostics(element, "Clear", description, () =>
            {
                SmartDesktopActions.Retry(() => DesktopUiQuery.SetValue(FindElementOnce(element), string.Empty, clearFirst: true));
            });
        }

        public static string GetText(DesktopElement element, string? description = null)
        {
            string result = string.Empty;
            ExecuteWithDiagnostics(element, "GetText", description, () =>
            {
                result = element.GetText();
            });
            return result;
        }

        public static string GetAttribute(DesktopElement element, string attributeName, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
                throw new ArgumentException("Attribute name cannot be null or empty.", nameof(attributeName));

            string result = string.Empty;
            ExecuteWithDiagnostics(element, "GetAttribute", description ?? $"GetAttribute '{attributeName}'", () =>
            {
                result = ReadProperty(FindElementOnce(element), attributeName);
            });
            return result;
        }

        public static string? GetProperty(DesktopElement element, string propertyName, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            string? result = null;
            ExecuteWithDiagnostics(element, "GetProperty", description ?? $"GetProperty '{propertyName}'", () =>
            {
                result = ReadProperty(FindElementOnce(element), propertyName);
            });
            return result;
        }

        public static bool IsDisplayed(DesktopElement element, string? description = null)
        {
            bool result = false;
            ExecuteWithDiagnostics(element, "IsDisplayed", description, () =>
            {
                var target = FindElementOnce(element);
                result = target.IsAvailable && !target.IsOffscreen;
            });
            return result;
        }

        public static bool IsEnabled(DesktopElement element, string? description = null)
        {
            bool result = false;
            ExecuteWithDiagnostics(element, "IsEnabled", description, () =>
            {
                result = FindElementOnce(element).IsEnabled;
            });
            return result;
        }

        public static bool IsSelected(DesktopElement element, string? description = null)
        {
            bool result = false;
            ExecuteWithDiagnostics(element, "IsSelected", description, () =>
            {
                var target = FindElementOnce(element);
                if (target.Patterns.SelectionItem.TryGetPattern(out var selection))
                {
                    result = selection.IsSelected;
                    return;
                }

                if (target.Patterns.Toggle.TryGetPattern(out var toggle))
                {
                    result = toggle.ToggleState == FlaUI.Core.Definitions.ToggleState.On;
                    return;
                }

                result = false;
            });
            return result;
        }

        public static void Focus(DesktopElement element, string? description = null)
        {
            ExecuteWithDiagnostics(element, "Focus", description, () =>
            {
                SmartDesktopActions.Retry(() => FindElementOnce(element).Focus());
            });
        }

        public static void ScrollIntoView(DesktopElement element, int timeoutSeconds = 10, string? description = null)
        {
            ExecuteWithDiagnostics(element, "ScrollIntoView", description, () =>
            {
                SmartDesktopActions.Retry(() =>
                {
                    var target = FindElementOnce(element);
                    if (target.Patterns.ScrollItem.TryGetPattern(out var scrollItem))
                    {
                        scrollItem.ScrollIntoView();
                        return;
                    }

                    Mouse.MoveTo(target.GetClickablePoint());
                });
            });
        }

        public static void DragAndDrop(DesktopElement source, DesktopElement target, string? description = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            ExecuteWithDiagnostics(source, "DragAndDrop", description, () =>
            {
                SmartDesktopActions.Retry(() =>
                {
                    var src = FindElementOnce(source);
                    var dst = FindElementOnce(target);
                    Mouse.Drag(src.GetClickablePoint(), dst.GetClickablePoint());
                });
            });
        }

        public static void DragAndDropByOffset(DesktopElement source, int offsetX, int offsetY, string? description = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            ExecuteWithDiagnostics(source, "DragAndDropByOffset", description, () =>
            {
                SmartDesktopActions.Retry(() =>
                {
                    var src = FindElementOnce(source);
                    var start = src.GetClickablePoint();
                    var end = new Point(start.X + offsetX, start.Y + offsetY);
                    Mouse.Drag(start, end);
                });
            });
        }

        public static void Highlight(DesktopElement element, TimeSpan? duration = null, string? description = null)
        {
            ExecuteWithDiagnostics(element, "Highlight", description, () =>
            {
                var target = FindElementOnce(element);
                target.DrawHighlight();
                var wait = duration ?? TimeSpan.FromMilliseconds(500);
                if (wait > TimeSpan.Zero)
                {
                    System.Threading.Thread.Sleep(wait);
                }
            });
        }

        public static void RetryClick(DesktopElement element, int maxAttempts, string? description = null)
        {
            ExecuteWithDiagnostics(element, "RetryClick", description, () =>
            {
                SmartDesktopActions.Retry(() => element.Click(), retries: maxAttempts);
            });
        }

        public static void RetrySendKeys(DesktopElement element, string text, int maxAttempts, string? description = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            ExecuteWithDiagnostics(element, "RetrySendKeys", description ?? $"RetrySendKeys '{text}'", () =>
            {
                SmartDesktopActions.Retry(
                    () => DesktopUiQuery.SetValue(FindElementOnce(element), text, clearFirst: true),
                    retries: maxAttempts);
            });
        }

        private static AutomationElement FindElementOnce(DesktopElement element)
        {
            return SelfHealingLocatorEngine.FindElement(element.Session, element.Locator);
        }

        private static string ReadProperty(AutomationElement element, string name)
        {
            switch (name.Trim().ToLowerInvariant())
            {
                case "name":
                    return element.Name ?? string.Empty;
                case "automationid":
                case "accessibilityid":
                case "id":
                    return element.AutomationId ?? string.Empty;
                case "classname":
                case "class":
                    return element.ClassName ?? string.Empty;
                case "controltype":
                    return element.ControlType.ToString();
                case "helptext":
                    return element.HelpText ?? string.Empty;
                case "isenabled":
                    return element.IsEnabled.ToString();
                case "isoffscreen":
                    return element.IsOffscreen.ToString();
                case "value":
                    return DesktopUiQuery.ReadText(element);
                case "frameworkid":
                    return element.FrameworkAutomationElement.Properties.FrameworkId.ValueOrDefault ?? string.Empty;
                default:
                    return DesktopUiQuery.ReadText(element);
            }
        }

        private static void ExecuteWithDiagnostics(DesktopElement element, string actionName, string? description, Action action)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var message = description ?? actionName;

            Logger.Information("Desktop action '{Action}' started. Description: {Description}", actionName, message);
            ExtentReportManager.LogInfo($"Desktop action '{actionName}' - {message}");

            StepResult? stepResult = null;

            try
            {
                try
                {
                    stepResult = new StepResult { name = $"Desktop action: {actionName}" };
                    AllureLifecycle.Instance.StartStep(stepResult);
                }
                catch
                {
                    // If Allure is not configured, continue without nested steps
                }

                action();

                Logger.Information("Desktop action '{Action}' completed successfully.", actionName);
                ExtentReportManager.LogPass($"Desktop action '{actionName}' completed successfully.");

                if (stepResult != null)
                {
                    try
                    {
                        AllureLifecycle.Instance.UpdateStep(s => s.status = Status.passed);
                    }
                    catch { /* ignore Allure errors */ }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Desktop action '{Action}' failed: {Error}", actionName, ex.Message);
                ExtentReportManager.LogFail($"Desktop action '{actionName}' failed: {ex.Message}");

                TryCaptureAndAttachScreenshot(element, actionName);

                if (stepResult != null)
                {
                    try
                    {
                        AllureLifecycle.Instance.UpdateStep(s =>
                        {
                            s.status = Status.failed;
                            s.statusDetails = new StatusDetails { message = ex.Message, trace = ex.StackTrace };
                        });
                    }
                    catch { /* ignore Allure errors */ }
                }

                throw;
            }
            finally
            {
                if (stepResult != null)
                {
                    try
                    {
                        AllureLifecycle.Instance.StopStep();
                    }
                    catch { /* ignore Allure errors */ }
                }
            }
        }

        private static void TryCaptureAndAttachScreenshot(DesktopElement element, string actionName)
        {
            try
            {
                var config = TestConfiguration.Instance;
                if (!config.DesktopCaptureActionScreenshots)
                {
                    return;
                }

                var screenshotsRoot = config.ScreenshotsPath;
                if (!Path.IsPathRooted(screenshotsRoot))
                {
                    screenshotsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), screenshotsRoot));
                }

                Directory.CreateDirectory(screenshotsRoot);

                var safeName = string.Concat(actionName.Split(Path.GetInvalidFileNameChars()));
                if (safeName.Length > 80)
                {
                    safeName = safeName[..80];
                }

                var fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}_desktop.png";
                var fullPath = Path.Combine(screenshotsRoot, fileName);

                element.Session.CaptureScreenshot(fullPath);

                Logger.Information("Desktop action screenshot captured: {Path}", fullPath);
                ExtentReportManager.AddScreenshot(fullPath, $"Desktop action '{actionName}'");

                try
                {
                    var screenshotBytes = File.ReadAllBytes(fullPath);
                    AllureApi.AddAttachment($"Desktop action '{actionName}'", "image/png", screenshotBytes);
                    Logger.Debug("Desktop action screenshot added to Allure for '{Action}'.", actionName);
                }
                catch (Exception allureEx)
                {
                    Logger.Warning("Failed to add desktop action screenshot to Allure: {Error}", allureEx.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to capture desktop action screenshot: {Error}", ex.Message);
            }
        }
    }
}

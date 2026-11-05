using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Drivers
{
    /// <summary>
    /// FlaUI application session: launched/attached process, UI Automation backend, and current window.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class DesktopSession : IDisposable
    {
        private bool _disposed;

        public DesktopSession(Application application, AutomationBase automation, Window currentWindow)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
            Automation = automation ?? throw new ArgumentNullException(nameof(automation));
            CurrentWindow = currentWindow ?? throw new ArgumentNullException(nameof(currentWindow));
        }

        public Application Application { get; }

        public AutomationBase Automation { get; }

        public Window CurrentWindow { get; private set; }

        /// <summary>
        /// Search root for locator lookups (the current top-level window).
        /// </summary>
        public AutomationElement SearchRoot => CurrentWindow;

        public string Title
        {
            get
            {
                try
                {
                    return CurrentWindow?.Title ?? Application.Name ?? string.Empty;
                }
                catch
                {
                    return Application.Name ?? string.Empty;
                }
            }
        }

        public int ProcessId => Application.ProcessId;

        public Window[] GetTopLevelWindows()
        {
            return Application.GetAllTopLevelWindows(Automation) ?? Array.Empty<Window>();
        }

        public bool SwitchToWindow(string windowTitle, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                throw new ArgumentException("Window title cannot be null or empty.", nameof(windowTitle));
            }

            var wait = timeout ?? TimeSpan.FromSeconds(10);
            var found = Retry.WhileNull(
                () =>
                {
                    var match = GetTopLevelWindows()
                        .FirstOrDefault(w =>
                            !string.IsNullOrWhiteSpace(w.Title) &&
                            w.Title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        return match;
                    }

                    return Automation.GetDesktop()
                        .FindFirstDescendant(cf =>
                            cf.ByControlType(ControlType.Window).And(cf.ByName(windowTitle)))
                        ?.AsWindow();
                },
                wait,
                TimeSpan.FromMilliseconds(250),
                throwOnTimeout: false,
                ignoreException: true).Result;

            if (found == null)
            {
                return false;
            }

            SetCurrentWindow(found);
            return true;
        }

        public bool SwitchToLatestWindow()
        {
            var windows = GetTopLevelWindows();
            if (windows.Length == 0)
            {
                var desktopWindows = Automation.GetDesktop()
                    .FindAllChildren(cf =>
                        cf.ByControlType(ControlType.Window).And(cf.ByProcessId(Application.ProcessId)));
                windows = desktopWindows.Select(e => e.AsWindow()).ToArray();
            }

            if (windows.Length == 0)
            {
                return false;
            }

            SetCurrentWindow(windows[^1]);
            return true;
        }

        public void MaximizeCurrentWindow()
        {
            if (CurrentWindow.Patterns.Window.TryGetPattern(out var windowPattern))
            {
                windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
                return;
            }

            try
            {
                CurrentWindow.SetForeground();
            }
            catch (Exception ex)
            {
                Logger.Warning("Unable to maximize or foreground the current window: {Error}", ex.Message);
            }
        }

        public void SetCurrentWindow(Window window)
        {
            CurrentWindow = window ?? throw new ArgumentNullException(nameof(window));
            try
            {
                CurrentWindow.SetForeground();
            }
            catch (Exception ex)
            {
                Logger.Debug("SetForeground failed for window '{Title}': {Error}", CurrentWindow.Title, ex.Message);
            }
        }

        public string CaptureScreenshot(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Screenshot path cannot be empty.", nameof(filePath));
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                using var image = CurrentWindow.Capture();
                image.Save(filePath);
            }
            catch (Exception ex)
            {
                Logger.Warning("Window capture failed ({Error}); falling back to main screen.", ex.Message);
                Capture.MainScreen().ToFile(filePath);
            }

            return filePath;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                CurrentWindow?.Close();
            }
            catch (Exception ex)
            {
                Logger.Debug("Closing current window failed: {Error}", ex.Message);
            }

            try
            {
                if (!Application.HasExited)
                {
                    Application.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Closing application failed: {Error}", ex.Message);
                try
                {
                    Application.Kill();
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                Automation.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Debug("Disposing UI Automation failed: {Error}", ex.Message);
            }

            try
            {
                Application.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}

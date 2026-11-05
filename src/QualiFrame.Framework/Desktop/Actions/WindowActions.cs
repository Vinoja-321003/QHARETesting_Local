using System;
using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Actions for managing application windows (switching, maximizing, closing, etc.).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowActions
    {
        public static void SwitchToWindow(DesktopSession session, string windowTitle, string? description = null)
        {
            Logger.Information("WindowActions.SwitchToWindow started. Title={Title}, Description={Description}", windowTitle ?? string.Empty, description ?? string.Empty);
            ExtentReportManager.LogInfo($"WindowActions.SwitchToWindow - Title={windowTitle}, Description={description}");

            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                throw new ArgumentException("Window title cannot be null or empty.", nameof(windowTitle));
            }

            if (!session.SwitchToWindow(windowTitle))
            {
                throw new InvalidOperationException($"Window with title containing '{windowTitle}' was not found.");
            }

            ExtentReportManager.LogPass($"Switched to window '{session.Title}'.");
        }

        public static void SwitchToWindow(string windowTitle, string? description = null) =>
            SwitchToWindow(DesktopSessionFactory.GetSession(), windowTitle, description);

        public static void SwitchToLatestWindow(DesktopSession session, string? description = null)
        {
            Logger.Information("WindowActions.SwitchToLatestWindow started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"WindowActions.SwitchToLatestWindow - Description={description}");

            if (!session.SwitchToLatestWindow())
            {
                throw new InvalidOperationException("No additional top-level window was found to switch to.");
            }

            ExtentReportManager.LogPass($"Switched to latest window '{session.Title}'.");
        }

        public static void SwitchToLatestWindow(string? description = null) =>
            SwitchToLatestWindow(DesktopSessionFactory.GetSession(), description);

        public static void MaximizeWindow(DesktopSession session, string? description = null)
        {
            Logger.Information("WindowActions.MaximizeWindow started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"WindowActions.MaximizeWindow - Description={description}");

            session.MaximizeCurrentWindow();
            ExtentReportManager.LogPass("Current desktop window maximized.");
        }

        public static void MaximizeWindow(string? description = null) =>
            MaximizeWindow(DesktopSessionFactory.GetSession(), description);
    }
}

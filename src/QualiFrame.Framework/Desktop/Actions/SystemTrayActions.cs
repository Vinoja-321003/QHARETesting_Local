using System;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Actions for interacting with the system tray and notifications.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class SystemTrayActions
    {
        public static void ClickTrayIcon(string iconName, string? description = null)
        {
            Logger.Information("SystemTrayActions.ClickTrayIcon started. Icon={Icon}, Description={Description}", iconName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"SystemTrayActions.ClickTrayIcon - Icon={iconName}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var icon = FindTrayIcon(iconName)
                    ?? throw new InvalidOperationException($"System tray icon '{iconName}' was not found.");
                icon.Click();
            });
        }

        public static string GetTrayIconTooltip(string iconName, string? description = null)
        {
            Logger.Information("SystemTrayActions.GetTrayIconTooltip started. Icon={Icon}, Description={Description}", iconName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"SystemTrayActions.GetTrayIconTooltip - Icon={iconName}, Description={description}");

            string tooltip = string.Empty;
            SmartDesktopActions.Retry(() =>
            {
                var icon = FindTrayIcon(iconName)
                    ?? throw new InvalidOperationException($"System tray icon '{iconName}' was not found.");
                tooltip = icon.HelpText;
                if (string.IsNullOrWhiteSpace(tooltip))
                {
                    tooltip = DesktopUiQuery.ReadText(icon);
                }
            });
            return tooltip;
        }

        private static FlaUI.Core.AutomationElements.AutomationElement? FindTrayIcon(string iconName)
        {
            var session = DesktopSessionFactory.GetSession();
            var desktop = session.Automation.GetDesktop();
            var taskbar = DesktopUiQuery.ByClassName(desktop, "Shell_TrayWnd") ?? desktop;

            var areas = new[]
            {
                DesktopUiQuery.ByName(taskbar, "User Promoted Notification Area"),
                DesktopUiQuery.ByName(taskbar, "Notification Area"),
                DesktopUiQuery.ByName(taskbar, "Overflow Notification Area"),
                DesktopUiQuery.ByName(desktop, "System Tray Overflow Window")
            };

            foreach (var area in areas.Where(a => a != null))
            {
                var icon = DesktopUiQuery.ByName(area!, iconName);
                if (icon != null)
                {
                    return icon;
                }
            }

            return DesktopUiQuery.ByName(taskbar, iconName);
        }
    }
}

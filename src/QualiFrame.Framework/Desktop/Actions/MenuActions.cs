using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Actions for interacting with desktop menus and context menus.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class MenuActions
    {
        public static void OpenMenu(DesktopElement menuBar, string? description = null)
        {
            Logger.Information("MenuActions.OpenMenu started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"MenuActions.OpenMenu - Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var root = Resolve(menuBar);
                var firstItem = DesktopUiQuery.ByControlType(root, ControlType.MenuItem).FirstOrDefault() ?? root;
                firstItem.Click();
            });
        }

        public static void CloseMenu(DesktopElement menuBar, string? description = null)
        {
            Logger.Information("MenuActions.CloseMenu started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"MenuActions.CloseMenu - Description={description}");

            FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
            ExtentReportManager.LogPass("Menu closed via Escape.");
        }

        public static void SelectMenuItem(DesktopElement menuBar, string menuPath, string? description = null)
        {
            Logger.Information("MenuActions.SelectMenuItem started. Path={Path}, Description={Description}", menuPath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"MenuActions.SelectMenuItem - Path={menuPath}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var item = WalkMenu(menuBar, menuPath);
                DesktopUiQuery.TryInvoke(item);
            });
        }

        public static void RightClickMenuItem(DesktopElement menuBar, string menuPath, string? description = null)
        {
            Logger.Information("MenuActions.RightClickMenuItem started. Path={Path}, Description={Description}", menuPath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"MenuActions.RightClickMenuItem - Path={menuPath}, Description={description}");

            SmartDesktopActions.Retry(() => WalkMenu(menuBar, menuPath).RightClick());
        }

        public static bool IsMenuItemEnabled(DesktopElement menuBar, string menuPath, string? description = null)
        {
            Logger.Information("MenuActions.IsMenuItemEnabled started. Path={Path}, Description={Description}", menuPath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"MenuActions.IsMenuItemEnabled - Path={menuPath}, Description={description}");

            var enabled = false;
            SmartDesktopActions.Retry(() =>
            {
                enabled = WalkMenu(menuBar, menuPath).IsEnabled;
            });
            return enabled;
        }

        public static IReadOnlyList<string> GetMenuItems(DesktopElement menuRoot, string? description = null)
        {
            Logger.Information("MenuActions.GetMenuItems started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"MenuActions.GetMenuItems - Description={description}");

            IReadOnlyList<string> names = Array.Empty<string>();
            SmartDesktopActions.Retry(() =>
            {
                names = DesktopUiQuery.Names(DesktopUiQuery.ByControlType(Resolve(menuRoot), ControlType.MenuItem)).ToList();
            });
            return names;
        }

        private static AutomationElement Resolve(DesktopElement menuBar) =>
            SelfHealingLocatorEngine.FindElement(menuBar.Session, menuBar.Locator);

        private static AutomationElement WalkMenu(DesktopElement menuBar, string menuPath)
        {
            var root = Resolve(menuBar);
            var segments = (menuPath ?? string.Empty).Split(new[] { '/', '\\', '>' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                throw new ArgumentException("Menu path cannot be empty.", nameof(menuPath));
            }

            AutomationElement current = root;
            foreach (var segment in segments)
            {
                var next = DesktopUiQuery.ByName(current, segment)
                           ?? DesktopUiQuery.ByName(menuBar.Session.Automation.GetDesktop(), segment)
                           ?? throw new InvalidOperationException($"Menu item '{segment}' was not found in path '{menuPath}'.");
                next.Click();
                current = next;
            }

            return current;
        }
    }
}

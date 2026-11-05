using System;
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
    /// Actions for interacting with tabbed panels and tab controls.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class TabbedPanelActions
    {
        public static void SelectTab(DesktopElement tabRoot, string tabName, string? description = null)
        {
            Logger.Information("TabbedPanelActions.SelectTab started. Tab={Tab}, Description={Description}", tabName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TabbedPanelActions.SelectTab - Tab={tabName}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var tab = FindTab(Resolve(tabRoot), tabName);
                if (tab.Patterns.SelectionItem.TryGetPattern(out var selection))
                {
                    selection.Select();
                }
                else
                {
                    tab.Click();
                }
            });
        }

        public static void CloseTab(DesktopElement tabRoot, string tabName, string? description = null)
        {
            Logger.Information("TabbedPanelActions.CloseTab started. Tab={Tab}, Description={Description}", tabName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TabbedPanelActions.CloseTab - Tab={tabName}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var tab = FindTab(Resolve(tabRoot), tabName);
                var close = DesktopUiQuery.FirstByNames(tab, "Close", "Close tab")
                            ?? DesktopUiQuery.ByControlType(tab, ControlType.Button).FirstOrDefault();
                if (close != null)
                {
                    DesktopUiQuery.TryInvoke(close);
                    return;
                }

                tab.RightClick();
                var closeItem = DesktopUiQuery.ByName(tabRoot.Session.CurrentWindow, "Close");
                if (closeItem == null)
                {
                    throw new InvalidOperationException($"Could not find a close control for tab '{tabName}'.");
                }

                DesktopUiQuery.TryInvoke(closeItem);
            });
        }

        public static string GetSelectedTab(DesktopElement tabRoot, string? description = null)
        {
            Logger.Information("TabbedPanelActions.GetSelectedTab started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"TabbedPanelActions.GetSelectedTab - Description={description}");

            string name = string.Empty;
            SmartDesktopActions.Retry(() =>
            {
                var tabs = GetTabs(Resolve(tabRoot));
                var selected = tabs.FirstOrDefault(t =>
                    t.Patterns.SelectionItem.TryGetPattern(out var selection) && selection.IsSelected);
                name = selected != null ? DesktopUiQuery.ReadText(selected) : string.Empty;
            });
            return name;
        }

        public static int GetTabCount(DesktopElement tabRoot, string? description = null)
        {
            Logger.Information("TabbedPanelActions.GetTabCount started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"TabbedPanelActions.GetTabCount - Description={description}");

            var count = 0;
            SmartDesktopActions.Retry(() =>
            {
                count = GetTabs(Resolve(tabRoot)).Length;
            });
            return count;
        }

        private static AutomationElement Resolve(DesktopElement tabRoot) =>
            SelfHealingLocatorEngine.FindElement(tabRoot.Session, tabRoot.Locator);

        private static AutomationElement[] GetTabs(AutomationElement root)
        {
            var tabs = DesktopUiQuery.ByControlType(root, ControlType.TabItem);
            return tabs.Length > 0 ? tabs : DesktopUiQuery.ByControlType(root, ControlType.Button);
        }

        private static AutomationElement FindTab(AutomationElement root, string tabName)
        {
            var tab = GetTabs(root).FirstOrDefault(t =>
                string.Equals(DesktopUiQuery.ReadText(t).Trim(), tabName, StringComparison.OrdinalIgnoreCase))
                ?? DesktopUiQuery.ByName(root, tabName);

            return tab ?? throw new InvalidOperationException($"Tab '{tabName}' was not found.");
        }
    }
}

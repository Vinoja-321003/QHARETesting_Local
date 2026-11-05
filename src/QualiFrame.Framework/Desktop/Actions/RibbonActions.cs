using System;
using System.Runtime.Versioning;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Actions for interacting with Ribbon-based desktop UIs.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class RibbonActions
    {
        public static void SelectRibbonTab(DesktopElement ribbonRoot, string tabName, string? description = null)
        {
            Logger.Information("RibbonActions.SelectRibbonTab started. Tab={Tab}, Description={Description}", tabName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"RibbonActions.SelectRibbonTab - Tab={tabName}, Description={description}");

            TabbedPanelActions.SelectTab(ribbonRoot, tabName, description);
        }

        public static void ClickRibbonButton(DesktopElement ribbonRoot, string tabName, string groupName, string buttonName, string? description = null)
        {
            Logger.Information("RibbonActions.ClickRibbonButton started. Tab={Tab}, Group={Group}, Button={Button}, Description={Description}", tabName, groupName, buttonName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"RibbonActions.ClickRibbonButton - Tab={tabName}, Group={groupName}, Button={buttonName}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var root = SelfHealingLocatorEngine.FindElement(ribbonRoot.Session, ribbonRoot.Locator);
                TabbedPanelActions.SelectTab(ribbonRoot, tabName, description);
                var group = DesktopUiQuery.ByName(root, groupName) ?? root;
                var button = DesktopUiQuery.ByName(group, buttonName)
                             ?? DesktopUiQuery.ByName(root, buttonName)
                             ?? throw new InvalidOperationException($"Ribbon button '{buttonName}' was not found on tab '{tabName}' / group '{groupName}'.");
                DesktopUiQuery.TryInvoke(button);
            });
        }

        public static bool IsRibbonButtonEnabled(DesktopElement ribbonRoot, string tabName, string groupName, string buttonName, string? description = null)
        {
            Logger.Information("RibbonActions.IsRibbonButtonEnabled started. Tab={Tab}, Group={Group}, Button={Button}, Description={Description}", tabName, groupName, buttonName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"RibbonActions.IsRibbonButtonEnabled - Tab={tabName}, Group={groupName}, Button={buttonName}, Description={description}");

            var enabled = false;
            SmartDesktopActions.Retry(() =>
            {
                var root = SelfHealingLocatorEngine.FindElement(ribbonRoot.Session, ribbonRoot.Locator);
                TabbedPanelActions.SelectTab(ribbonRoot, tabName, description);
                var group = DesktopUiQuery.ByName(root, groupName) ?? root;
                var button = DesktopUiQuery.ByName(group, buttonName)
                             ?? DesktopUiQuery.ByName(root, buttonName)
                             ?? throw new InvalidOperationException($"Ribbon button '{buttonName}' was not found on tab '{tabName}' / group '{groupName}'.");
                enabled = button.IsEnabled;
            });
            return enabled;
        }
    }
}

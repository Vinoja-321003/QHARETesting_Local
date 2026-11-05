using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Actions for interacting with file dialogs and file explorer-like UIs.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class FileExplorerActions
    {
        public static void OpenFileDialog(DesktopElement triggerElement, string? description = null)
        {
            Logger.Information("FileExplorerActions.OpenFileDialog started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"FileExplorerActions.OpenFileDialog - Description={description}");

            DesktopActions.Click(triggerElement, description ?? "Open file dialog");
        }

        public static void SelectFile(DesktopElement dialogRoot, string fileName, string? description = null)
        {
            Logger.Information("FileExplorerActions.SelectFile started. File={File}, Description={Description}", fileName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"FileExplorerActions.SelectFile - File={fileName}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var root = SelfHealingLocatorEngine.FindElement(dialogRoot.Session, dialogRoot.Locator);
                var item = DesktopUiQuery.ByName(root, fileName)
                           ?? throw new InvalidOperationException($"File '{fileName}' was not found in the dialog.");
                item.Click();
                var open = DesktopUiQuery.FirstByNames(root, "Open", "Select")
                           ?? DesktopUiQuery.ByControlType(root, ControlType.Button).FirstOrDefault();
                if (open != null)
                {
                    DesktopUiQuery.TryInvoke(open);
                }
            });
        }

        public static void EnterFileName(DesktopElement dialogRoot, string fileName, string? description = null)
        {
            Logger.Information("FileExplorerActions.EnterFileName started. File={File}, Description={Description}", fileName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"FileExplorerActions.EnterFileName - File={fileName}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var root = SelfHealingLocatorEngine.FindElement(dialogRoot.Session, dialogRoot.Locator);
                var edit = DesktopUiQuery.ByAutomationId(root, "1148")
                           ?? DesktopUiQuery.ByName(root, "File name:")
                           ?? DesktopUiQuery.ByControlType(root, ControlType.Edit).FirstOrDefault()
                           ?? DesktopUiQuery.ByControlType(root, ControlType.ComboBox).FirstOrDefault()
                           ?? throw new InvalidOperationException("Could not locate the file name field in the dialog.");
                DesktopUiQuery.SetValue(edit, fileName, clearFirst: true);
            });
        }

        public static IReadOnlyList<string> GetFileList(DesktopElement dialogRoot, string? description = null)
        {
            Logger.Information("FileExplorerActions.GetFileList started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"FileExplorerActions.GetFileList - Description={description}");

            IReadOnlyList<string> names = Array.Empty<string>();
            SmartDesktopActions.Retry(() =>
            {
                var root = SelfHealingLocatorEngine.FindElement(dialogRoot.Session, dialogRoot.Locator);
                var items = DesktopUiQuery.ByControlType(root, ControlType.ListItem);
                if (items.Length == 0)
                {
                    items = DesktopUiQuery.ByControlType(root, ControlType.DataItem);
                }

                names = DesktopUiQuery.Names(items).ToList();
            });
            return names;
        }
    }
}

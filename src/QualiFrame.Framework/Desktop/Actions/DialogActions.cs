using System;
using System.Runtime.Versioning;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Actions for interacting with modal and modeless dialogs in desktop applications.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class DialogActions
    {
        public static void WaitForDialog(string dialogTitle, int timeoutSeconds = 10, string? description = null)
        {
            Logger.Information("DialogActions.WaitForDialog started. Title={Title}, Timeout={Timeout}s, Description={Description}", dialogTitle ?? string.Empty, timeoutSeconds, description ?? string.Empty);
            ExtentReportManager.LogInfo($"DialogActions.WaitForDialog - Title={dialogTitle}, Timeout={timeoutSeconds}s, Description={description}");

            if (string.IsNullOrWhiteSpace(dialogTitle))
            {
                throw new ArgumentException("Dialog title cannot be null or empty.", nameof(dialogTitle));
            }

            var session = DesktopSessionFactory.GetSession();
            var timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds));
            var switched = Retry.WhileFalse(
                () => session.SwitchToWindow(dialogTitle, TimeSpan.FromMilliseconds(500)),
                timeout,
                TimeSpan.FromMilliseconds(250),
                throwOnTimeout: false,
                ignoreException: true).Result;

            if (!switched)
            {
                throw new TimeoutException($"Dialog with title containing '{dialogTitle}' was not found within {timeoutSeconds} seconds.");
            }

            Logger.Information("DialogActions.WaitForDialog: Dialog with title containing '{Title}' is now present.", dialogTitle);
            ExtentReportManager.LogPass($"DialogActions.WaitForDialog - Dialog with title containing '{dialogTitle}' is present.");
        }

        public static void AcceptDialog(DesktopElement dialogRoot, string? description = null)
        {
            Logger.Information("DialogActions.AcceptDialog started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"DialogActions.AcceptDialog - Description={description}");

            if (dialogRoot == null) throw new ArgumentNullException(nameof(dialogRoot));

            SmartDesktopActions.Retry(() =>
            {
                var rootElement = SelfHealingLocatorEngine.FindElement(dialogRoot.Session, dialogRoot.Locator);
                var okButton = DesktopUiQuery.FirstByNames(rootElement, "OK", "Ok", "Yes", "Accept")
                               ?? DesktopUiQuery.ByControlType(rootElement, ControlType.Button).FirstOrNull();

                if (okButton == null)
                {
                    throw new InvalidOperationException("Could not locate an OK/Yes button in the dialog to accept it.");
                }

                DesktopUiQuery.TryInvoke(okButton);
                Logger.Information("DialogActions.AcceptDialog: Clicked OK/Yes button in dialog.");
                ExtentReportManager.LogPass("DialogActions.AcceptDialog - Dialog accepted via OK/Yes button.");
            });
        }

        public static void DismissDialog(DesktopElement dialogRoot, string? description = null)
        {
            Logger.Information("DialogActions.DismissDialog started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"DialogActions.DismissDialog - Description={description}");

            if (dialogRoot == null) throw new ArgumentNullException(nameof(dialogRoot));

            SmartDesktopActions.Retry(() =>
            {
                var rootElement = SelfHealingLocatorEngine.FindElement(dialogRoot.Session, dialogRoot.Locator);
                var cancelButton = DesktopUiQuery.FirstByNames(rootElement, "Cancel", "No", "Close", "Dismiss");
                if (cancelButton != null)
                {
                    DesktopUiQuery.TryInvoke(cancelButton);
                }
                else
                {
                    Keyboard.Type(VirtualKeyShort.ESCAPE);
                }

                Logger.Information("DialogActions.DismissDialog: Dialog dismissed.");
                ExtentReportManager.LogPass("DialogActions.DismissDialog - Dialog dismissed.");
            });
        }

        public static void EnterDialogText(DesktopElement dialogRoot, string fieldLabelOrId, string value, string? description = null)
        {
            Logger.Information("DialogActions.EnterDialogText started. Field={Field}, Value={Value}, Description={Description}", fieldLabelOrId, value, description ?? string.Empty);
            ExtentReportManager.LogInfo($"DialogActions.EnterDialogText - Field={fieldLabelOrId}, Value={value}, Description={description}");

            if (dialogRoot == null) throw new ArgumentNullException(nameof(dialogRoot));
            if (string.IsNullOrWhiteSpace(fieldLabelOrId)) throw new ArgumentException("Field label or id is required.", nameof(fieldLabelOrId));

            SmartDesktopActions.Retry(() =>
            {
                var rootElement = SelfHealingLocatorEngine.FindElement(dialogRoot.Session, dialogRoot.Locator);
                var field = DesktopUiQuery.ByAutomationId(rootElement, fieldLabelOrId)
                            ?? DesktopUiQuery.ByName(rootElement, fieldLabelOrId)
                            ?? DesktopUiQuery.ByControlType(rootElement, ControlType.Edit).FirstOrNull();

                if (field == null)
                {
                    throw new InvalidOperationException($"Could not locate dialog field '{fieldLabelOrId}'.");
                }

                DesktopUiQuery.SetValue(field, value, clearFirst: true);
                ExtentReportManager.LogPass($"Entered text into dialog field '{fieldLabelOrId}'.");
            });
        }

        private static FlaUI.Core.AutomationElements.AutomationElement? FirstOrNull(this FlaUI.Core.AutomationElements.AutomationElement[] items)
            => items.Length > 0 ? items[0] : null;
    }
}

using System;
using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;

namespace QualiFrame.Framework.Desktop.Pages
{
    /// <summary>
    /// Example desktop login window page object.
    /// Update AutomationIds/Names to match the real application under test.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LoginWindow : DesktopPage
    {
        public LoginWindow(DesktopSession session) : base(session)
        {
        }

        private DesktopElement UsernameTextBox => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "UsernameTextBox" });

        private DesktopElement PasswordTextBox => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "PasswordTextBox" });

        private DesktopElement LoginButton => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "LoginButton" });

        public override void WaitForLoaded(int timeoutSeconds = 10)
        {
            WaitUntilVisible(
                s => s.SearchRoot.FindFirstDescendant(cf => cf.ByAutomationId("UsernameTextBox")),
                timeoutSeconds);
        }

        public void EnterCredentials(string username, string password)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (password is null) throw new ArgumentNullException(nameof(password));

            QualiFrame.Framework.Desktop.Actions.DesktopActions.SendKeys(UsernameTextBox, username, clearBefore: true, description: "Enter username");
            QualiFrame.Framework.Desktop.Actions.DesktopActions.SendKeys(PasswordTextBox, password, clearBefore: true, description: "Enter password");
        }

        public void ClickLogin()
        {
            LoginButton.Click();
        }
    }
}

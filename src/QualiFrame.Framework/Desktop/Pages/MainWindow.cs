using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;

namespace QualiFrame.Framework.Desktop.Pages
{
    /// <summary>
    /// Example main window page object.
    /// Update element identifiers to reflect your real desktop application.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class MainWindow : DesktopPage
    {
        public MainWindow(DesktopSession session) : base(session)
        {
        }

        private DesktopElement MainContent => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "MainContentRoot" });

        public override void WaitForLoaded(int timeoutSeconds = 10)
        {
            WaitUntilVisible(
                s => s.SearchRoot.FindFirstDescendant(cf => cf.ByAutomationId("MainContentRoot")),
                timeoutSeconds);
        }

        public bool IsVisible()
        {
            try
            {
                return QualiFrame.Framework.Desktop.Actions.DesktopActions.IsDisplayed(MainContent, "Check main window visibility");
            }
            catch
            {
                return false;
            }
        }
    }
}

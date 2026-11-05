using FluentAssertions;
using Reqnroll;
using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Flows;
using QualiFrame.Framework.Reports;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    [SupportedOSPlatform("windows")]
    public class DesktopSteps
    {
        private readonly DesktopLoginFlows _loginFlows;

        public DesktopSteps()
        {
            _loginFlows = new DesktopLoginFlows();
        }

        [Given(@"the desktop application is running")]
        public void GivenTheDesktopApplicationIsRunning()
        {
            // Accessing flows ensures the driver and login window are created.
            ExtentReportManager.LogInfo("Desktop application session initialized via FlaUI.");
        }

        [When(@"I login to the desktop app with username ""(.*)"" and password ""(.*)""")]
        public void WhenILoginToTheDesktopAppWithUsernameAndPassword(string username, string password)
        {
            _loginFlows.LoginAs(username, password);
            ExtentReportManager.LogPass($"Desktop login attempted for user '{username}'.");
        }

        [Then(@"I should see the desktop main window")]
        public void ThenIShouldSeeTheDesktopMainWindow()
        {
            var visible = _loginFlows.IsMainWindowVisible();
            visible.Should().BeTrue("the main desktop window should be visible after successful login");
            ExtentReportManager.LogPass("Desktop main window is visible.");
        }
    }
}


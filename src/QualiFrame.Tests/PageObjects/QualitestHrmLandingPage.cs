using OpenQA.Selenium;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Authenticated landing page for the Qualitest HRM application.
    /// Locators were captured from the dashboard at qualitest-hrm-ui-production.up.railway.app.
    /// </summary>
    public class QualitestHrmLandingPage : BasePage
    {
        public const string LandingUrlPath = "/dashboard";
        public const string LandingHeadingText = "Welcome to QualitestHRM Dashboard";

        private readonly By _landingHeading = By.CssSelector("[data-testid='qualitesthrm-dashboard-welcome']");
        private readonly By _logoutButton = By.CssSelector("[data-testid='qualitesthrm-layout-logout']");

        public override void WaitForPageToLoad()
        {
            WaitForPageLoad();
            WaitForUrlToContain(LandingUrlPath, timeoutSeconds: 15);
            WaitForElementToBeVisible(_landingHeading, timeoutSeconds: 15);
            WaitForElementToBeVisible(_logoutButton, timeoutSeconds: 15);
        }

        public override bool IsPageLoaded()
        {
            return IsLandingPageDisplayed();
        }

        public bool IsAuthenticated()
        {
            try
            {
                WaitForUrlToContain(LandingUrlPath, timeoutSeconds: 15);
                WaitForElementToBeVisible(_logoutButton, timeoutSeconds: 15);
                return GetCurrentUrl().Contains(LandingUrlPath, StringComparison.OrdinalIgnoreCase)
                    && IsElementDisplayed(_logoutButton);
            }
            catch
            {
                return false;
            }
        }

        public bool IsLandingPageDisplayed()
        {
            try
            {
                WaitForPageToLoad();
                return GetCurrentUrl().Contains(LandingUrlPath, StringComparison.OrdinalIgnoreCase)
                    && IsElementDisplayed(_landingHeading)
                    && string.Equals(GetText(_landingHeading).Trim(), LandingHeadingText, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public bool HasLeftLoginPage()
        {
            return !GetCurrentUrl().Contains(QualitestHrmLoginPage.LoginUrlPath, StringComparison.OrdinalIgnoreCase);
        }

        public void RefreshCurrentPage()
        {
            RefreshPage();
            WaitForPageToLoad();
            Logger.Information("Refreshed the Qualitest HRM landing page.");
        }

        public void OpenAuthenticatedApplication()
        {
            var current = new Uri(GetCurrentUrl());
            var dashboardUrl = new Uri(current.GetLeftPart(UriPartial.Authority) + LandingUrlPath);
            NavigateToUrl(dashboardUrl.AbsoluteUri);
            WaitForPageToLoad();
            Logger.Information("Opened the Qualitest HRM dashboard from the stored session.");
        }
    }
}

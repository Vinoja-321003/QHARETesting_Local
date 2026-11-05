using OpenQA.Selenium;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Page object for the OrangeHRM dashboard and profile menu.
    /// Locators were captured from the live OrangeHRM OS 5.9 demo application.
    /// </summary>
    public class OrangeHrmDashboardPage : BasePage
    {
        public const string DashboardUrlPath = "/web/index.php/dashboard/index";

        private readonly By _dashboardHeading = By.CssSelector("h6.oxd-topbar-header-breadcrumb-module");
        private readonly By _profileMenu = By.CssSelector(".oxd-userdropdown-tab");
        private readonly By _logoutLink = By.CssSelector("a.oxd-userdropdown-link[href*='/auth/logout']");

        public override void WaitForPageToLoad()
        {
            WaitForPageLoad();
            WaitForUrlToContain(DashboardUrlPath, timeoutSeconds: 15);
            WaitForElementToBeVisible(_dashboardHeading, timeoutSeconds: 15);
        }

        public override bool IsPageLoaded()
        {
            return IsDashboardDisplayed();
        }

        public bool IsRedirectedToDashboard()
        {
            try
            {
                WaitForUrlToContain(DashboardUrlPath, timeoutSeconds: 15);
                return GetCurrentUrl().Contains(DashboardUrlPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public bool IsDashboardDisplayed()
        {
            try
            {
                WaitForPageToLoad();
                return GetCurrentUrl().Contains(DashboardUrlPath, StringComparison.OrdinalIgnoreCase)
                    && IsElementDisplayed(_dashboardHeading)
                    && string.Equals(GetText(_dashboardHeading).Trim(), "Dashboard", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public bool IsProfileMenuVisible()
        {
            try
            {
                WaitForElementToBeVisible(_profileMenu, timeoutSeconds: 10);
                return IsElementDisplayed(_profileMenu);
            }
            catch
            {
                return false;
            }
        }

        public void ClickProfileMenu()
        {
            WaitForElementToBeClickable(_profileMenu, timeoutSeconds: 10);
            ClickElement(_profileMenu);
            WaitForElementToBeVisible(_logoutLink, timeoutSeconds: 10);
            Logger.Information("Opened the OrangeHRM profile menu.");
        }

        public void ClickLogout()
        {
            WaitForElementToBeClickable(_logoutLink, timeoutSeconds: 10);
            ClickElement(_logoutLink);
            Logger.Information("Clicked OrangeHRM Logout.");
        }
    }
}

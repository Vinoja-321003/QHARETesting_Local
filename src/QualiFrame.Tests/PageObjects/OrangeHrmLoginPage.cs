using OpenQA.Selenium;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Page object for the OrangeHRM login screen.
    /// Locators were captured from the live OrangeHRM OS 5.9 demo application.
    /// </summary>
    public class OrangeHrmLoginPage : BasePage
    {
        public const string LoginUrlPath = "/web/index.php/auth/login";

        private readonly By _logo = By.CssSelector("img[alt='company-branding']");
        private readonly By _usernameField = By.Name("username");
        private readonly By _passwordField = By.Name("password");
        private readonly By _loginButton = By.CssSelector("button.orangehrm-login-button[type='submit']");
        private readonly By _forgotPasswordLink = By.CssSelector("p.orangehrm-login-forgot-header");

        public void NavigateToLoginPage(string url)
        {
            NavigateToUrl(url);
            WaitForPageToLoad();
            Logger.Information("Navigated to OrangeHRM login URL: {Url}", url);
        }

        public override void WaitForPageToLoad()
        {
            WaitForPageLoad();
            WaitForUrlToContain(LoginUrlPath, timeoutSeconds: 15);
            WaitForElementToBeVisible(_usernameField, timeoutSeconds: 15);
        }

        public override bool IsPageLoaded()
        {
            return IsLoginPageDisplayed();
        }

        public bool IsLoginPageDisplayed()
        {
            try
            {
                WaitForPageToLoad();
                return GetCurrentUrl().Contains(LoginUrlPath, StringComparison.OrdinalIgnoreCase)
                    && IsElementDisplayed(_usernameField)
                    && IsElementDisplayed(_passwordField)
                    && IsElementDisplayed(_loginButton);
            }
            catch
            {
                return false;
            }
        }

        public bool IsLogoVisible()
        {
            return IsVisible(_logo);
        }

        public bool IsUsernameFieldVisibleAndEnabled()
        {
            return IsVisibleAndEnabled(_usernameField);
        }

        public bool IsPasswordFieldVisibleAndEnabled()
        {
            return IsVisibleAndEnabled(_passwordField);
        }

        public bool IsLoginButtonVisibleAndClickable()
        {
            try
            {
                WaitForElementToBeVisible(_loginButton, timeoutSeconds: 10);
                WaitForElementToBeClickable(_loginButton, timeoutSeconds: 10);
                return IsElementDisplayed(_loginButton) && IsElementEnabled(_loginButton);
            }
            catch
            {
                return false;
            }
        }

        public bool IsForgotPasswordLinkVisible()
        {
            return IsVisible(_forgotPasswordLink);
        }

        public void EnterUsername(string username)
        {
            WaitForElementToBeVisible(_usernameField, timeoutSeconds: 10);
            SendKeys(_usernameField, username);
            Logger.Information("Entered OrangeHRM username.");
        }

        public void EnterPassword(string password)
        {
            WaitForElementToBeVisible(_passwordField, timeoutSeconds: 10);
            SendKeys(_passwordField, password);
            Logger.Information("Entered OrangeHRM password.");
        }

        public void ClickLoginButton()
        {
            WaitForElementToBeClickable(_loginButton, timeoutSeconds: 10);
            ClickElement(_loginButton);
            Logger.Information("Clicked the OrangeHRM Login button.");
        }

        public bool IsRedirectedToLoginPage()
        {
            try
            {
                WaitForUrlToContain(LoginUrlPath, timeoutSeconds: 15);
                return GetCurrentUrl().Contains(LoginUrlPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsVisible(By locator)
        {
            try
            {
                WaitForElementToBeVisible(locator, timeoutSeconds: 10);
                return IsElementDisplayed(locator);
            }
            catch
            {
                return false;
            }
        }

        private bool IsVisibleAndEnabled(By locator)
        {
            try
            {
                WaitForElementToBeVisible(locator, timeoutSeconds: 10);
                return IsElementDisplayed(locator) && IsElementEnabled(locator);
            }
            catch
            {
                return false;
            }
        }
    }
}

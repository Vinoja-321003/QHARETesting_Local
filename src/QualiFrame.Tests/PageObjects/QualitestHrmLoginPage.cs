using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Login page for the Qualitest HRM application targeted by QualitestHRM-Login.feature.
    /// Locators were captured from https://qualitest-hrm-ui-production.up.railway.app/login.
    /// </summary>
    public class QualitestHrmLoginPage : BasePage
    {
        public const string LoginUrlPath = "/login";
        public const string AuthenticationErrorText = "Invalid credentials";

        private readonly By _loginPage = By.CssSelector("[data-testid='qualitesthrm-auth-login-page']");
        private readonly By _usernameField = By.CssSelector("[data-testid='qualitesthrm-login-username']");
        private readonly By _passwordField = By.CssSelector("[data-testid='qualitesthrm-login-password']");
        private readonly By _loginButton = By.CssSelector("[data-testid='qualitesthrm-login-submit']");
        private readonly By _authenticationError = By.CssSelector("[data-testid='qualitesthrm-auth-login-error']");

        private string? _loginPageUrl;

        public void NavigateToLoginPage(string url)
        {
            _loginPageUrl = url;
            NavigateToUrl(url);
            WaitForPageToLoad();
            Logger.Information("Navigated to the Qualitest HRM login page.");
        }

        public void ReturnToLoginPage()
        {
            if (string.IsNullOrWhiteSpace(_loginPageUrl))
            {
                throw new InvalidOperationException(
                    "The Qualitest HRM login page URL was not captured. Open the login page before returning to it.");
            }

            NavigateToUrl(_loginPageUrl);
            WaitForPageLoad();
            Logger.Information("Requested the Qualitest HRM login page again.");
        }

        public override void WaitForPageToLoad()
        {
            WaitForPageLoad();
            WaitForUrlToContain(LoginUrlPath, timeoutSeconds: 15);
            WaitForElementToBeVisible(_loginPage, timeoutSeconds: 15);
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

        public bool IsUnauthenticated()
        {
            try
            {
                var currentUrl = GetCurrentUrl();
                return currentUrl.Contains(LoginUrlPath, StringComparison.OrdinalIgnoreCase)
                    && !currentUrl.Contains(QualitestHrmLandingPage.LandingUrlPath, StringComparison.OrdinalIgnoreCase)
                    && IsElementDisplayed(_usernameField)
                    && IsElementDisplayed(_passwordField);
            }
            catch
            {
                return false;
            }
        }

        public void EnterUsername(string username)
        {
            WaitForElementToBeVisible(_usernameField, timeoutSeconds: 10);
            SendKeys(_usernameField, username ?? string.Empty);
            Logger.Information("Entered the Qualitest HRM username.");
        }

        public void EnterPassword(string password)
        {
            WaitForElementToBeVisible(_passwordField, timeoutSeconds: 10);
            SendKeys(_passwordField, password ?? string.Empty);
            Logger.Information("Entered the Qualitest HRM password.");
        }

        public void SubmitLogin()
        {
            WaitForElementToBeVisible(_loginButton, timeoutSeconds: 10);
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            try
            {
                wait.Until(_ => Driver.FindElement(_loginButton).Enabled);
            }
            catch (WebDriverTimeoutException)
            {
                Logger.Information("The Qualitest HRM login request stayed blocked because Login is disabled.");
                return;
            }

            ClickElement(_loginButton);
            Logger.Information("Submitted the Qualitest HRM login request.");
        }

        public bool IsLoginRequestBlocked()
        {
            try
            {
                WaitForElementToBeVisible(_loginButton, timeoutSeconds: 10);
                return !Driver.FindElement(_loginButton).Enabled;
            }
            catch
            {
                return false;
            }
        }

        public string GetUsernameValue()
        {
            WaitForElementToBeVisible(_usernameField, timeoutSeconds: 10);
            return GetAttribute(_usernameField, "value") ?? string.Empty;
        }

        public string GetPasswordValue()
        {
            WaitForElementToBeVisible(_passwordField, timeoutSeconds: 10);
            return GetAttribute(_passwordField, "value") ?? string.Empty;
        }

        public bool IsPasswordMasked()
        {
            try
            {
                WaitForElementToBeVisible(_passwordField, timeoutSeconds: 10);
                var inputType = GetAttribute(_passwordField, "type");
                return string.Equals(inputType, "password", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public bool IsAuthenticationErrorDisplayed()
        {
            try
            {
                WaitForElementToBeVisible(_authenticationError, timeoutSeconds: 10);
                return GetText(_authenticationError)
                    .Contains(AuthenticationErrorText, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public bool IsLoginFormAbsent()
        {
            try
            {
                WaitForElementToDisappear(_usernameField, timeoutSeconds: 10);
                return !IsElementPresent(_usernameField)
                    && !IsElementPresent(_passwordField)
                    && !IsElementPresent(_loginButton);
            }
            catch
            {
                return false;
            }
        }
    }
}

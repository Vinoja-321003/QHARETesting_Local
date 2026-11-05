using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Core;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Tests.Examples
{
    /// <summary>
    /// Example modular test demonstrating the use of ModularTestCase base class.
    /// This test follows the Template Method pattern with SetUp, ExecuteTest, and TearDown methods.
    /// </summary>
    public class LoginModularTest : ModularTestCase
    {
        private string? _username;
        private string? _password;
        private LoginPage? _loginPage;
        private HomePage? _homePage;

        /// <summary>
        /// Setup method called after base setup is complete.
        /// Loads test data and initializes page objects.
        /// </summary>
        public override void SetUp()
        {
            Logger.Information("Setting up LoginModularTest");

            // Load test data using test case ID
            var testData = GetAllTestData("LoginTest");
            if (testData != null)
            {
                _username = testData.TryGetValue("Username", out var username) ? username?.ToString() : null;
                _password = testData.TryGetValue("Password", out var password) ? password?.ToString() : null;
            }

            // Fallback to default credentials if test data not found
            _username ??= Config.DefaultUsername;
            _password ??= Config.DefaultPassword;

            // Initialize page objects
            _loginPage = new LoginPage(Driver!);
            _homePage = new HomePage(Driver!);

            Logger.Information("LoginModularTest setup completed. Username: {Username}", _username);
        }

        /// <summary>
        /// Test execution method containing the actual test logic.
        /// </summary>
        public override void ExecuteTest()
        {
            Logger.Information("Executing LoginModularTest");

            // Step 1: Verify login page is displayed
            Assert.That(_loginPage!.IsDisplayed(), Is.True, "Login page should be displayed");

            // Step 2: Enter credentials
            _loginPage.EnterUsername(_username!);
            _loginPage.EnterPassword(_password!);

            // Step 3: Click login button
            _loginPage.ClickLogin();

            // Step 4: Verify home page is displayed
            Assert.That(_homePage!.IsDisplayed(), Is.True, "Home page should be displayed after successful login");
            Assert.That(_homePage.GetPageTitle(), Is.EqualTo("Products"), "Page title should be 'Products'");

            Logger.Information("LoginModularTest execution completed successfully");
        }

        /// <summary>
        /// Teardown method called before base teardown begins.
        /// Performs any necessary cleanup.
        /// </summary>
        public override void TearDown()
        {
            Logger.Information("Tearing down LoginModularTest");

            // Cleanup: Clear cookies if needed
            try
            {
                Driver?.Manage().Cookies.DeleteAllCookies();
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to clear cookies during teardown: {Error}", ex.Message);
            }

            Logger.Information("LoginModularTest teardown completed");
        }
    }

    // Placeholder page objects - these would be implemented in the actual PageObjects folder
    internal class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly By _usernameField = By.Id("user-name");
        private readonly By _passwordField = By.Id("password");
        private readonly By _loginButton = By.Id("login-button");

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public bool IsDisplayed()
        {
            try
            {
                return _driver.FindElement(_loginButton).Displayed;
            }
            catch
            {
                return false;
            }
        }

        public void EnterUsername(string username)
        {
            _driver.FindElement(_usernameField).SendKeys(username);
        }

        public void EnterPassword(string password)
        {
            _driver.FindElement(_passwordField).SendKeys(password);
        }

        public void ClickLogin()
        {
            _driver.FindElement(_loginButton).Click();
        }
    }

    internal class HomePage
    {
        private readonly IWebDriver _driver;
        private readonly By _pageTitle = By.ClassName("title");

        public HomePage(IWebDriver driver)
        {
            _driver = driver;
        }

        public bool IsDisplayed()
        {
            try
            {
                return _driver.FindElement(_pageTitle).Displayed;
            }
            catch
            {
                return false;
            }
        }

        public string GetPageTitle()
        {
            return _driver.FindElement(_pageTitle).Text;
        }
    }
}

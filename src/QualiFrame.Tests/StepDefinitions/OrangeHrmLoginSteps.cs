using NUnit.Framework;
using QualiFrame.Tests.PageObjects;
using Reqnroll;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    public class OrangeHrmLoginSteps
    {
        private readonly OrangeHrmLoginPage _loginPage;
        private readonly OrangeHrmDashboardPage _dashboardPage;

        public OrangeHrmLoginSteps(OrangeHrmLoginPage loginPage, OrangeHrmDashboardPage dashboardPage)
        {
            _loginPage = loginPage;
            _dashboardPage = dashboardPage;
        }

        [Given(@"the user navigates to ""(.*)""")]
        public void GivenTheUserNavigatesTo(string url)
        {
            _loginPage.NavigateToLoginPage(url);
        }

        [Given(@"the login page is displayed")]
        [Then(@"the login page should be displayed")]
        public void ThenTheLoginPageShouldBeDisplayed()
        {
            Assert.That(
                _loginPage.IsLoginPageDisplayed(),
                Is.True,
                "Expected the OrangeHRM login page to be displayed.");
        }

        [Then(@"the OrangeHRM logo should be visible")]
        public void ThenTheOrangeHrmLogoShouldBeVisible()
        {
            Assert.That(
                _loginPage.IsLogoVisible(),
                Is.True,
                "Expected the OrangeHRM company branding logo to be visible.");
        }

        [Then(@"the Username textbox should be visible and enabled")]
        public void ThenTheUsernameTextboxShouldBeVisibleAndEnabled()
        {
            Assert.That(
                _loginPage.IsUsernameFieldVisibleAndEnabled(),
                Is.True,
                "Expected the Username textbox to be visible and enabled.");
        }

        [Then(@"the Password textbox should be visible and enabled")]
        public void ThenThePasswordTextboxShouldBeVisibleAndEnabled()
        {
            Assert.That(
                _loginPage.IsPasswordFieldVisibleAndEnabled(),
                Is.True,
                "Expected the Password textbox to be visible and enabled.");
        }

        [Then(@"the Login button should be visible and clickable")]
        public void ThenTheLoginButtonShouldBeVisibleAndClickable()
        {
            Assert.That(
                _loginPage.IsLoginButtonVisibleAndClickable(),
                Is.True,
                "Expected the Login button to be visible and clickable.");
        }

        [Then(@"the Forgot your password link should be visible")]
        public void ThenTheForgotYourPasswordLinkShouldBeVisible()
        {
            Assert.That(
                _loginPage.IsForgotPasswordLinkVisible(),
                Is.True,
                "Expected the Forgot your password link to be visible.");
        }

        [When(@"the user enters username ""(.*)""")]
        public void WhenTheUserEntersUsername(string username)
        {
            _loginPage.EnterUsername(username);
        }

        [When(@"the user enters password ""(.*)""")]
        public void WhenTheUserEntersPassword(string password)
        {
            _loginPage.EnterPassword(password);
        }

        [When(@"the user clicks the Login button")]
        public void WhenTheUserClicksTheLoginButton()
        {
            _loginPage.ClickLoginButton();
        }

        [Then(@"the user should be redirected to the Dashboard page")]
        public void ThenTheUserShouldBeRedirectedToTheDashboardPage()
        {
            Assert.That(
                _dashboardPage.IsRedirectedToDashboard(),
                Is.True,
                "Expected the browser to redirect to the OrangeHRM Dashboard page.");
        }

        [Then(@"the Dashboard page should be displayed")]
        public void ThenTheDashboardPageShouldBeDisplayed()
        {
            Assert.That(
                _dashboardPage.IsDashboardDisplayed(),
                Is.True,
                "Expected the OrangeHRM Dashboard page to be displayed.");
        }

        [Then(@"the profile menu should be visible")]
        public void ThenTheProfileMenuShouldBeVisible()
        {
            Assert.That(
                _dashboardPage.IsProfileMenuVisible(),
                Is.True,
                "Expected the OrangeHRM profile menu to be visible.");
        }

        [When(@"the user clicks on the profile menu")]
        public void WhenTheUserClicksOnTheProfileMenu()
        {
            _dashboardPage.ClickProfileMenu();
        }

        [When(@"the user clicks on Logout")]
        public void WhenTheUserClicksOnLogout()
        {
            _dashboardPage.ClickLogout();
        }

        [Then(@"the user should be redirected to the Login page")]
        public void ThenTheUserShouldBeRedirectedToTheLoginPage()
        {
            Assert.That(
                _loginPage.IsRedirectedToLoginPage(),
                Is.True,
                "Expected the browser to redirect to the OrangeHRM Login page.");
        }
    }
}

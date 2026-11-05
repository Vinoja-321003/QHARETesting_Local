using NUnit.Framework;
using QualiFrame.Tests.PageObjects;
using Reqnroll;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    [Scope(Feature = "HRM Login Functionality")]
    public class QualitestHrmLoginSteps
    {
        private readonly QualitestHrmLoginPage _loginPage;
        private readonly QualitestHrmLandingPage _landingPage;
        private string _username = string.Empty;
        private string _password = string.Empty;

        public QualitestHrmLoginSteps(QualitestHrmLoginPage loginPage, QualitestHrmLandingPage landingPage)
        {
            _loginPage = loginPage;
            _landingPage = landingPage;
        }

        [Given(@"the user navigates to the HRM login page ""(.*)""")]
        public void GivenTheUserNavigatesToTheHrmLoginPage(string url)
        {
            _loginPage.NavigateToLoginPage(url);
        }

        [Given(@"the HRM login page is displayed")]
        [Then(@"the HRM login page should remain displayed")]
        public void ThenTheHrmLoginPageShouldBeDisplayed()
        {
            Assert.That(
                _loginPage.IsLoginPageDisplayed(),
                Is.True,
                "Expected the Qualitest HRM login page to be displayed.");
        }

        [Given(@"the user is not authenticated")]
        [Then(@"the user should remain unauthenticated")]
        public void ThenTheUserShouldRemainUnauthenticated()
        {
            Assert.That(
                _loginPage.IsUnauthenticated(),
                Is.True,
                "Expected the user to remain on the Qualitest HRM login page without an authenticated session.");
        }

        [When(@"the user enters username ""(.*)""")]
        public void WhenTheUserEntersUsername(string username)
        {
            _username = username ?? string.Empty;
            _loginPage.EnterUsername(_username);
        }

        [When(@"the user enters password ""(.*)""")]
        public void WhenTheUserEntersPassword(string password)
        {
            _password = password ?? string.Empty;
            _loginPage.EnterPassword(_password);
        }

        [When(@"the user submits the login request")]
        public void WhenTheUserSubmitsTheLoginRequest()
        {
            _loginPage.SubmitLogin();
        }

        [Then(@"the entered password should be masked")]
        public void ThenTheEnteredPasswordShouldBeMasked()
        {
            Assert.That(
                _loginPage.IsPasswordMasked(),
                Is.True,
                "Expected the Qualitest HRM password field to mask the entered value.");
        }

        [Then(@"the user should be authenticated successfully")]
        [Then(@"the user should remain authenticated")]
        public void ThenTheUserShouldBeAuthenticatedSuccessfully()
        {
            Assert.That(
                _landingPage.IsAuthenticated(),
                Is.True,
                "Expected the Qualitest HRM session to be authenticated.");
        }

        [Then(@"the authenticated application landing page should be displayed")]
        public void ThenTheAuthenticatedApplicationLandingPageShouldBeDisplayed()
        {
            Assert.That(
                _landingPage.IsLandingPageDisplayed(),
                Is.True,
                "Expected the authenticated Qualitest HRM landing page to be displayed.");
        }

        [Then(@"the user should no longer be on the login page")]
        public void ThenTheUserShouldNoLongerBeOnTheLoginPage()
        {
            Assert.That(
                _landingPage.HasLeftLoginPage(),
                Is.True,
                "Expected the browser to leave the Qualitest HRM login page after authentication.");
        }

        [Then(@"an authentication error message should be displayed")]
        public void ThenAnAuthenticationErrorMessageShouldBeDisplayed()
        {
            Assert.That(
                _loginPage.IsAuthenticationErrorDisplayed(),
                Is.True,
                $"Expected the Qualitest HRM authentication error \"{QualitestHrmLoginPage.AuthenticationErrorText}\".");
        }

        [Then(@"the login request should remain blocked because required credentials are missing")]
        public void ThenTheLoginRequestShouldRemainBlockedBecauseRequiredCredentialsAreMissing()
        {
            var usernameMissing = string.IsNullOrWhiteSpace(_username);
            var passwordMissing = string.IsNullOrWhiteSpace(_password);

            Assert.That(
                usernameMissing || passwordMissing,
                Is.True,
                "Expected at least one mandatory Qualitest HRM credential to be blank.");
            Assert.That(
                _loginPage.IsLoginRequestBlocked(),
                Is.True,
                "Expected the Qualitest HRM Login button to stay disabled until the required credentials are entered.");

            if (usernameMissing)
            {
                Assert.That(
                    _loginPage.GetUsernameValue(),
                    Is.Empty,
                    "Expected the Qualitest HRM username field to be empty.");
            }

            if (passwordMissing)
            {
                Assert.That(
                    _loginPage.GetPasswordValue(),
                    Is.Empty,
                    "Expected the Qualitest HRM password field to be empty.");
            }
        }

        [When(@"the user refreshes the current page")]
        public void WhenTheUserRefreshesTheCurrentPage()
        {
            _landingPage.RefreshCurrentPage();
        }

        [Then(@"the login form should not be displayed")]
        public void ThenTheLoginFormShouldNotBeDisplayed()
        {
            Assert.That(
                _loginPage.IsLoginFormAbsent(),
                Is.True,
                "Expected the Qualitest HRM login form to be hidden for an authenticated user.");
        }

        [When(@"the user navigates back to the HRM login page")]
        public void WhenTheUserNavigatesBackToTheHrmLoginPage()
        {
            _loginPage.ReturnToLoginPage();
        }

        [When(@"the user opens the authenticated application again")]
        public void WhenTheUserOpensTheAuthenticatedApplicationAgain()
        {
            _landingPage.OpenAuthenticatedApplication();
        }
    }
}

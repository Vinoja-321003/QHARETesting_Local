using Reqnroll;
using QualiFrame.Tests.PageObjects;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities;
using QualiFrame.Framework.Utilities.Logging;
using NUnit.Framework;
using System;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    public class WebSteps
    {
        private readonly WebPage _webPage;
        private readonly TestConfiguration _config;
        private readonly ScenarioContext _scenarioContext;

        public WebSteps(WebPage webPage, ScenarioContext scenarioContext)
        {
            _webPage = webPage;
            _config = TestConfiguration.Instance;
            _scenarioContext = scenarioContext;
        }

        [Given(@"Launch Application using ""(.*)""")]
        public void GivenLaunchApplicationUsing(string tcId)
        {
            _scenarioContext["TCID"] = tcId;
            _webPage.LaunchApp();
        }

        [Given(@"the application is ready for testing")]
        public void GivenTheApplicationIsReadyForTesting()
        {
            // Initialize test environment and prepare for testing
            Logger.Information("Application is ready for testing");
        }

        [When(@"Launch Application using ""(.*)""")]
        public void WhenLaunchApplicationUsing(string tcId)
        {
            _scenarioContext["TCID"] = tcId;
            _webPage.LaunchApp();
        }

        [Then(@"verify login page displayed successfully")]
        public void ThenVerifyLoginPageDisplayedSuccessfully()
        {
            _webPage.VerifyLogin();
        }

        [When(@"User enters userCredentials in text field")]
        public void WhenUserEntersUserCredentialsInTextField()
        {
            // Using hardcoded test data for framework readiness
            string userName = _config.DefaultUsername;
            string password = _config.DefaultPassword;
            _webPage.EnterUserName(userName);
            _webPage.EnterPassword(password);
        }

        [When(@"User enters invalid userCredentials in text field")]
        public void WhenUserEntersInvalidUserCredentialsInTextField()
        {
            // Enter invalid credentials to trigger error message
            _webPage.EnterUserName("invalid_user");
            _webPage.EnterPassword("invalid_password");
        }

        [When(@"User clicks Login button")]
        public void WhenUserClicksLoginButton()
        {
            _webPage.ClickLoginButton();
        }

        [Then(@"verify home page displayed successfully")]
        public void ThenVerifyHomePageDisplayedSuccessfully()
        {
            _webPage.VerifyHomePage();
        }

        [When(@"User clicks SauceLabBackPack button")]
        public void WhenUserClicksSauceLabBackPackButton()
        {
            _webPage.ClickSauceLabBackPackButton();
        }

        [Then(@"User should see SauceLabBackPack Page")]
        public void ThenUserShouldSeeSauceLabBackPackPage()
        {
            _webPage.VerifySauceLabBackPackPage();
        }

        [When(@"User clicks AddToCart button")]
        public void WhenUserClicksAddToCartButton()
        {
            _webPage.ClickAddToCartButton();
        }

        [When(@"User clicks Cart button")]
        public void WhenUserClicksCartButton()
        {
            _webPage.ClickCartButton();
        }

        [Then(@"User should see SauceLabBackPack Product")]
        public void ThenUserShouldSeeSauceLabBackPackProduct()
        {
            _webPage.VerifySauceLabBackPackProduct();
        }

        [When(@"User should see SauceLabBackPack Product")]
        public void WhenUserShouldSeeSauceLabBackPackProduct()
        {
            _webPage.VerifySauceLabBackPackProduct();
        }

        [When(@"User clicks Remove button")]
        public void WhenUserClicksRemoveButton()
        {
            _webPage.ClickRemoveButton();
        }

        [Then(@"User should see NoProduct in Cart")]
        public void ThenUserShouldSeeNoProductInCart()
        {
            _webPage.VerifyNoProductInCart();
        }

        [When(@"User clicks TopMenu button")]
        public void WhenUserClicksTopMenuButton()
        {
            _webPage.ClickTopMenuButton();
        }

        [When(@"User clicks AllItem button")]
        public void WhenUserClicksAllItemButton()
        {
            _webPage.ClickAllItemButton();
        }

        [When(@"User clicks SauceLabBikeLight button")]
        public void WhenUserClicksSauceLabBikeLightButton()
        {
            _webPage.ClickSauceLabBikeLightButton();
        }

        [Then(@"User should see SauceLabBikeLight Page")]
        public void ThenUserShouldSeeSauceLabBikeLightPage()
        {
            _webPage.VerifySauceLabBikeLightPage();
        }

        [When(@"User clicks CheckOut button")]
        public void WhenUserClicksCheckOutButton()
        {
            _webPage.ClickCheckOutButton();
        }

        [When(@"User enters Checkout information")]
        public void WhenUserEntersCheckoutInformation()
        {
            _webPage.EnterCheckoutInformation();
        }

        [When(@"User clicks Continue button")]
        public void WhenUserClicksContinueButton()
        {
            _webPage.ClickContinueButton();
        }

        [When(@"User clicks Finish button")]
        public void WhenUserClicksFinishButton()
        {
            _webPage.ClickFinishButton();
        }

        [Then(@"User should see ThankYouOrder Page")]
        public void ThenUserShouldSeeThankYouOrderPage()
        {
            _webPage.VerifyThankYouOrderPage();
        }

        [When(@"User clicks LogOut button")]
        public void WhenUserClicksLogOutButton()
        {
            _webPage.ClickLogOutButton();
        }

        [Then(@"verify error message displayed successfully")]
        public void ThenVerifyErrorMessageDisplayedSuccessfully()
        {
            _webPage.VerifyErrorMessage();
        }

        [Given(@"I am on the login page")]
        public void GivenIAmOnTheLoginPage()
        {
            _webPage.LaunchApp();
            _webPage.VerifyLogin();
        }

        [When(@"I enter valid credentials")]
        public void WhenIEnterValidCredentials()
        {
            _webPage.EnterUserName(_config.DefaultUsername);
            _webPage.EnterPassword(_config.DefaultPassword);
        }

        [When(@"I enter invalid credentials")]
        public void WhenIEnterInvalidCredentials()
        {
            _webPage.EnterUserName("invalid_user");
            _webPage.EnterPassword("invalid_password");
        }

        [When(@"I click the login button")]
        public void WhenIClickTheLoginButton()
        {
            _webPage.ClickLoginButton();
        }

        [Then(@"I should be logged in successfully")]
        public void ThenIShouldBeLoggedInSuccessfully()
        {
            _webPage.VerifyHomePage();
        }

        [Then(@"I should see an error message")]
        public void ThenIShouldSeeAnErrorMessage()
        {
            _webPage.VerifyErrorMessage();
        }

        [Given(@"I am logged in")]
        public void GivenIAmLoggedIn()
        {
            _webPage.LaunchApp();
            _webPage.EnterUserName(_config.DefaultUsername);
            _webPage.EnterPassword(_config.DefaultPassword);
            _webPage.ClickLoginButton();
            _webPage.VerifyHomePage();
        }

        [When(@"I add a product to cart")]
        public void WhenIAddAProductToCart()
        {
            _webPage.ClickSauceLabBackPackButton();
            _webPage.VerifySauceLabBackPackPage();
            _webPage.ClickAddToCartButton();
        }

        [Then(@"the product should be in my cart")]
        public void ThenTheProductShouldBeInMyCart()
        {
            _webPage.ClickCartButton();
            _webPage.VerifySauceLabBackPackProduct();
        }

        [When(@"I remove the product from cart")]
        public void WhenIRemoveTheProductFromCart()
        {
            _webPage.ClickRemoveButton();
        }

        [Then(@"the product should not be in my cart")]
        public void ThenTheProductShouldNotBeInMyCart()
        {
            _webPage.VerifyNoProductInCart();
        }

        [When(@"I complete the checkout process")]
        public void WhenICompleteTheCheckoutProcess()
        {
            _webPage.ClickCheckOutButton();
            _webPage.EnterCheckoutInformation();
            _webPage.ClickContinueButton();
            _webPage.ClickFinishButton();
        }

        [Then(@"I should see the thank you page")]
        public void ThenIShouldSeeTheThankYouPage()
        {
            _webPage.VerifyThankYouOrderPage();
        }

        [When(@"I log out")]
        public void WhenILogOut()
        {
            _webPage.ClickTopMenuButton();
            _webPage.ClickLogOutButton();
        }

        [Then(@"I should be on the login page")]
        public void ThenIShouldBeOnTheLoginPage()
        {
            _webPage.VerifyLogin();
        }

        [Then(@"Verify swtich tab issue")]
        public void ThenSwitchTabIssue(){
            _webPage.SwitchTabIssue();
         }

        // NOTE: Hooks are now in QualiFrame.Tests.Hooks namespace
        // All lifecycle hooks (BeforeScenario, AfterScenario, BeforeFeature, AfterFeature)
        // are handled by QualiFrame.Tests.Hooks.Hooks class
    }
}

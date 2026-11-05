using NUnit.Framework;
using QualiFrame.Tests.PageObjects;
using Reqnroll;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    public class ShopifyHomePageNavigationSteps
    {
        private readonly ShopifyHomePage _shopifyHomePage;

        public ShopifyHomePageNavigationSteps(ShopifyHomePage shopifyHomePage)
        {
            _shopifyHomePage = shopifyHomePage;
        }

        [Given(@"user navigates to ""(.*)""")]
        public void GivenUserNavigatesTo(string url)
        {
            _shopifyHomePage.NavigateToStorefront(url);
        }

        [Given(@"user navigates to the homepage")]
        public void GivenUserNavigatesToTheHomepage()
        {
            _shopifyHomePage.NavigateToHomePage();
        }

        [Given(@"user is on the homepage")]
        public void GivenUserIsOnTheHomepage()
        {
            _shopifyHomePage.NavigateToHomePage();
        }

        [Then(@"the homepage should be displayed")]
        public void ThenTheHomepageShouldBeDisplayed()
        {
            Assert.That(
                _shopifyHomePage.IsHomePageDisplayed(),
                Is.True,
                "Expected the Sauce Demo Shopify homepage to be displayed.");
        }

        [Then(@"the navigation menu should contain ""(.*)""")]
        public void ThenTheNavigationMenuShouldContain(string menuItem)
        {
            Assert.That(
                _shopifyHomePage.IsNavigationMenuItemVisible(menuItem),
                Is.True,
                $"Expected the Shopify homepage navigation menu to contain '{menuItem}'.");
        }

        [Then(@"products should be visible on the homepage")]
        public void ThenProductsShouldBeVisibleOnTheHomepage()
        {
            Assert.That(
                _shopifyHomePage.AreHomePageProductsVisible(),
                Is.True,
                "Expected one or more products to be visible on the Shopify homepage.");
        }

        [When(@"user clicks on a product")]
        public void WhenUserClicksOnAProduct()
        {
            _shopifyHomePage.ClickFirstHomePageProduct();
        }

        [Then(@"user should be navigated to the product detail page")]
        public void ThenUserShouldBeNavigatedToTheProductDetailPage()
        {
            Assert.That(
                _shopifyHomePage.IsProductDetailPageDisplayed(),
                Is.True,
                "Expected the product detail page to be displayed after clicking a homepage product.");
        }
    }
}

using OpenQA.Selenium;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Linq;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Page object for the Sauce Demo Shopify storefront homepage.
    /// </summary>
    public class ShopifyHomePage : ComponentPage
    {
        public const string DefaultHomePageUrl = "https://sauce-demo.myshopify.com/";

        private readonly By _mainMenu = By.Id("main-");
        private readonly By _logoHomeLink = By.CssSelector("#logo a[href='']");
        private readonly By _homePageProductLinks = By.CssSelector("#page-content a[id^='-'][href*='//']");
        private readonly By _productDetailAddToCartButton = By.CssSelector("form[action='/cart/'] input[value='Add to ']");

        public void NavigateToStorefront(string url)
        {
            NavigateToUrl(url);
            WaitForPageToLoad();
            Logger.Information("Navigated to Shopify storefront URL: {Url}", url);
        }

        public void NavigateToHomePage()
        {
            NavigateToStorefront(DefaultHomePageUrl);
        }

        public override void WaitForPageToLoad()
        {
            WaitForPageLoad();
            WaitForElementToBeVisible(_mainMenu, timeoutSeconds: 10);
        }

        public override bool IsPageLoaded()
        {
            return IsElementDisplayed(_mainMenu);
        }

        public bool IsHomePageDisplayed()
        {
            WaitForPageToLoad();

            return IsElementDisplayed(_logoHomeLink)
                && IsElementDisplayed(_mainMenu)
                && GetCurrentUrl().StartsWith(DefaultHomePageUrl, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsNavigationMenuItemVisible(string menuItem)
        {
            var locator = NavigationMenuItemByText(menuItem);

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

        public bool AreHomePageProductsVisible()
        {
            try
            {
                WaitForElementToBePresent(_homePageProductLinks, timeoutSeconds: 10);
                return FindElements(_homePageProductLinks).Any(product => product.Displayed);
            }
            catch
            {
                return false;
            }
        }

        public void ClickFirstHomePageProduct()
        {
            WaitForElementToBeClickable(_homePageProductLinks, timeoutSeconds: 10);
            ClickElement(_homePageProductLinks);
            WaitForPageLoad();
            WaitForUrlToContain("/products/", timeoutSeconds: 10);
            Logger.Information("Clicked the first Shopify homepage product. Current URL: {Url}", GetCurrentUrl());
        }

        public bool IsProductDetailPageDisplayed()
        {
            try
            {
                WaitForPageLoad();
                WaitForElementToBeVisible(_productDetailAddToCartButton, timeoutSeconds: 10);

                return GetCurrentUrl().Contains("/products/", StringComparison.OrdinalIgnoreCase)
                    && IsElementDisplayed(_productDetailAddToCartButton);
            }
            catch
            {
                return false;
            }
        }

        private static By NavigationMenuItemByText(string menuItem)
        {
            return By.XPath($"//*[@id='main-menu']//a[normalize-space(.)={ToXPathLiteral(menuItem)}]");
        }

        private static string ToXPathLiteral(string value)
        {
            if (!value.Contains('\''))
            {
                return $"'{value}'";
            }

            if (!value.Contains('"'))
            {
                return $"\"{value}\"";
            }

            return "concat('" + value.Replace("'", "', \"'\", '") + "')";
        }
    }
}

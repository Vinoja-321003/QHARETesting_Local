using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable header component for web pages.
    /// </summary>
    public class HeaderComponent : BaseComponent
    {
        private readonly By _logoLocator;
        private readonly By _navigationMenuLocator;
        private readonly By _userMenuLocator;
        private readonly By _cartIconLocator;

        public HeaderComponent(
            IWebDriver driver,
            By logoLocator = null!,
            By navigationMenuLocator = null!,
            By userMenuLocator = null!,
            By cartIconLocator = null!)
            : base(driver)
        {
            _logoLocator = logoLocator ?? By.ClassName("app_logo");
            _navigationMenuLocator = navigationMenuLocator ?? By.ClassName("primary_header");
            _userMenuLocator = userMenuLocator ?? By.Id("react-burger-menu-btn");
            _cartIconLocator = cartIconLocator ?? By.ClassName("shopping_cart_link");
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_logoLocator).IsDisplayed();
            }
            catch
            {
                return false;
            }
        }

        public override void WaitForVisible(int timeoutSeconds = 10)
        {
            try
            {
                FindElement(_logoLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Header component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to wait for header component visibility");
                throw;
            }
        }

        public void ClickLogo()
        {
            FindElement(_logoLocator).Click();
            Logger.Debug("Clicked on header logo");
        }

        public void ClickNavigationMenu()
        {
            FindElement(_navigationMenuLocator).Click();
            Logger.Debug("Clicked on navigation menu");
        }

        public void ClickUserMenu()
        {
            FindElement(_userMenuLocator).Click();
            Logger.Debug("Clicked on user menu");
        }

        public void ClickCartIcon()
        {
            FindElement(_cartIconLocator).Click();
            Logger.Debug("Clicked on cart icon");
        }

        public int GetCartItemCount()
        {
            try
            {
                var badgeLocator = By.ClassName("shopping_cart_badge");
                var badgeText = FindElement(badgeLocator).GetText();
                return int.Parse(badgeText);
            }
            catch
            {
                return 0;
            }
        }
    }
}

using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Generic;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable navigation component for web pages.
    /// </summary>
    public class NavigationComponent : BaseComponent
    {
        private readonly By _navigationContainerLocator;
        private readonly Dictionary<string, By> _menuItemLocators;

        public NavigationComponent(
            IWebDriver driver,
            By navigationContainerLocator = null!)
            : base(driver)
        {
            _navigationContainerLocator = navigationContainerLocator ?? By.ClassName("bm-menu");
            _menuItemLocators = new Dictionary<string, By>();
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_navigationContainerLocator).IsDisplayed();
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
                FindElement(_navigationContainerLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Navigation component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to wait for navigation component visibility");
                throw;
            }
        }

        public void RegisterMenuItem(string menuItemName, By locator)
        {
            _menuItemLocators[menuItemName] = locator;
            Logger.Debug("Registered menu item '{MenuItemName}'", menuItemName);
        }

        public void ClickMenuItem(string menuItemName)
        {
            if (_menuItemLocators.TryGetValue(menuItemName, out var locator))
            {
                FindElement(locator).Click();
                Logger.Debug("Clicked on menu item '{MenuItemName}'", menuItemName);
            }
            else
            {
                throw new ArgumentException($"Menu item '{menuItemName}' not found", nameof(menuItemName));
            }
        }

        public IEnumerable<string> GetMenuItems()
        {
            return _menuItemLocators.Keys;
        }
    }
}

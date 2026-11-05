using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using QualiFrame.Framework.Mobile.Elements;
using QualiFrame.Framework.Utilities.Mobile;

namespace QualiFrame.Tests.PageObjects
{
    /// <summary>
    /// Named locators for the sample mobile scenarios. Prefer this over embedding ids in new steps.
    /// Existing id-based steps remain for backward compatibility.
    /// </summary>
    public class MobileAppScreen
    {
        private static readonly Dictionary<string, By> KnownLocators = new()
        {
            ["welcome"] = By.Id("welcome_screen"),
            ["username"] = By.Id("username_field"),
            ["password"] = By.Id("password_field"),
            ["login"] = By.Id("login_button"),
            ["dashboard"] = By.Id("dashboard"),
            ["menu"] = By.Id("menu_button"),
            ["settings"] = By.Id("settings_option"),
            ["settingsScreen"] = By.Id("settings_screen"),
            ["iosWelcome"] = By.Id("ios_welcome_screen"),
            ["iosUsername"] = By.Id("ios_username_field"),
            ["iosPassword"] = By.Id("ios_password_field"),
            ["iosLogin"] = By.Id("ios_login_button"),
            ["iosDashboard"] = By.Id("ios_dashboard")
        };

        public AppiumDriver Driver => AppiumDriverFactory.GetDriver();

        public MobileElement ElementById(string id) => new(Driver, By.Id(id), id);

        public MobileElement Named(string key)
        {
            if (!KnownLocators.TryGetValue(key, out var locator))
            {
                locator = By.Id(key);
            }

            return new MobileElement(Driver, locator, key);
        }

        public bool IsVisible(string id)
        {
            try
            {
                return ElementById(id).IsDisplayed();
            }
            catch
            {
                return false;
            }
        }
    }
}

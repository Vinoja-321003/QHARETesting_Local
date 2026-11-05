using System;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Framework.Screenplay.Abilities
{
    /// <summary>
    /// Ability to browse the web using a WebDriver.
    /// </summary>
    public class BrowseTheWeb : IAbility
    {
        public IWebDriver Driver { get; }

        public BrowseTheWeb(IWebDriver driver)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        /// <summary>
        /// Creates a BrowseTheWeb ability using the default WebDriver factory.
        /// </summary>
        public static BrowseTheWeb WithDefaultDriver()
        {
            var driver = EnhancedWebDriverFactory.CreateDriver();
            return new BrowseTheWeb(driver);
        }

        /// <summary>
        /// Creates a BrowseTheWeb ability using an existing WebDriver.
        /// </summary>
        public static BrowseTheWeb With(IWebDriver driver)
        {
            return new BrowseTheWeb(driver);
        }
    }
}

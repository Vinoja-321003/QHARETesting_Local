using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Framework.Web.Elements
{
    /// <summary>
    /// Utility class for element actions and operations.
    /// </summary>
    public static class ElementActions
    {
        public static IEnhancedElement FindElement(IWebDriver driver, By locator, string description = "")
        {
            return new EnhancedWebElement(driver, locator, description);
        }

        public static IEnhancedElement WrapElement(IWebDriver driver, IWebElement element, string description = "")
        {
            return new EnhancedWebElement(driver, element, description);
        }

        public static List<IEnhancedElement> FindElements(IWebDriver driver, By locator, string description = "")
        {
            var elements = driver.FindElements(locator);
            return elements.Select((element, index) => 
                (IEnhancedElement)new EnhancedWebElement(driver, element, $"{description}[{index}]")).ToList();
        }

        public static void WaitForElementToDisappear(IWebDriver driver, By locator, int timeoutSeconds = 10)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(locator));
        }

        public static void WaitForTextInElement(IWebDriver driver, By locator, string text, int timeoutSeconds = 10)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.TextToBePresentInElementLocated(locator, text));
        }
    }
}

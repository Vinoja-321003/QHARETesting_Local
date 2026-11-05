using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using QualiFrame.Framework.Web.Elements;

namespace QualiFrame.Framework.KeywordDriven.Keywords
{
    /// <summary>
    /// Keyword to click on an element.
    /// </summary>
    public class ClickKeyword : BaseKeyword
    {
        public override string KeywordName => "Click";

        protected override KeywordResult ExecuteKeyword(IWebDriver driver, Dictionary<string, object> parameters)
        {
            var locatorType = GetRequiredParameter<string>(parameters, "LocatorType");
            var locatorValue = GetRequiredParameter<string>(parameters, "LocatorValue");
            var timeout = GetOptionalParameter<int>(parameters, "Timeout", 10);

            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));

            try
            {
                var element = wait.Until(d => d.FindElement(by));
                var enhancedElement = new EnhancedWebElement(driver, by, $"Element: {locatorValue}");
                enhancedElement.Click();

                return KeywordResult.Pass($"Successfully clicked on element: {locatorValue}");
            }
            catch (Exception ex)
            {
                return KeywordResult.Fail($"Failed to click on element: {locatorValue}", ex);
            }
        }

        private By CreateLocator(string locatorType, string locatorValue)
        {
            return locatorType.ToLower() switch
            {
                "id" => By.Id(locatorValue),
                "name" => By.Name(locatorValue),
                "xpath" => By.XPath(locatorValue),
                "css" => By.CssSelector(locatorValue),
                "class" => By.ClassName(locatorValue),
                "linktext" => By.LinkText(locatorValue),
                "partiallinktext" => By.PartialLinkText(locatorValue),
                "tagname" => By.TagName(locatorValue),
                _ => throw new ArgumentException($"Unsupported locator type: {locatorType}")
            };
        }
    }
}

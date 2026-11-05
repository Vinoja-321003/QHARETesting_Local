using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace QualiFrame.Framework.KeywordDriven.Keywords
{
    /// <summary>
    /// Keyword to wait for an element or condition.
    /// </summary>
    public class WaitKeyword : BaseKeyword
    {
        public override string KeywordName => "Wait";

        protected override KeywordResult ExecuteKeyword(IWebDriver driver, Dictionary<string, object> parameters)
        {
            var waitType = GetRequiredParameter<string>(parameters, "WaitType");
            var timeout = GetOptionalParameter<int>(parameters, "Timeout", 10);

            try
            {
                switch (waitType.ToLower())
                {
                    case "time":
                        var seconds = GetRequiredParameter<int>(parameters, "Seconds");
                        Thread.Sleep(seconds * 1000);
                        return KeywordResult.Pass($"Waited for {seconds} seconds");

                    case "element":
                        var locatorType = GetRequiredParameter<string>(parameters, "LocatorType");
                        var locatorValue = GetRequiredParameter<string>(parameters, "LocatorValue");
                        var by = CreateLocator(locatorType, locatorValue);
                        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                        wait.Until(d => d.FindElement(by));
                        return KeywordResult.Pass($"Element found: {locatorValue}");

                    default:
                        return KeywordResult.Fail($"Unsupported wait type: {waitType}");
                }
            }
            catch (Exception ex)
            {
                return KeywordResult.Fail($"Wait operation failed: {ex.Message}", ex);
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

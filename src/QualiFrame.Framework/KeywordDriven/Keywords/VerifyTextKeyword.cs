using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace QualiFrame.Framework.KeywordDriven.Keywords
{
    /// <summary>
    /// Keyword to verify text in an element.
    /// </summary>
    public class VerifyTextKeyword : BaseKeyword
    {
        public override string KeywordName => "VerifyText";

        protected override KeywordResult ExecuteKeyword(IWebDriver driver, Dictionary<string, object> parameters)
        {
            var locatorType = GetRequiredParameter<string>(parameters, "LocatorType");
            var locatorValue = GetRequiredParameter<string>(parameters, "LocatorValue");
            var expectedText = GetRequiredParameter<string>(parameters, "ExpectedText");
            var timeout = GetOptionalParameter<int>(parameters, "Timeout", 10);
            var matchType = GetOptionalParameter<string>(parameters, "MatchType", "exact");

            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));

            try
            {
                var element = wait.Until(d => d.FindElement(by));
                var actualText = element.Text ?? string.Empty;

                bool matches = (matchType ?? "exact").ToLower() switch
                {
                    "exact" => actualText.Equals(expectedText, StringComparison.OrdinalIgnoreCase),
                    "contains" => actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
                    "starts" => actualText.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase),
                    "ends" => actualText.EndsWith(expectedText, StringComparison.OrdinalIgnoreCase),
                    _ => actualText.Equals(expectedText, StringComparison.OrdinalIgnoreCase)
                };

                if (matches)
                {
                    return KeywordResult.Pass($"Text verification passed. Expected: {expectedText}, Actual: {actualText}");
                }
                else
                {
                    return KeywordResult.Fail($"Text verification failed. Expected: {expectedText}, Actual: {actualText}");
                }
            }
            catch (Exception ex)
            {
                return KeywordResult.Fail($"Failed to verify text in element: {locatorValue}", ex);
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

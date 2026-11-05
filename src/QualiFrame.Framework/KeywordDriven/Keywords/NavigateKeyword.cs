using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;

namespace QualiFrame.Framework.KeywordDriven.Keywords
{
    /// <summary>
    /// Keyword to navigate to a URL.
    /// </summary>
    public class NavigateKeyword : BaseKeyword
    {
        public override string KeywordName => "Navigate";

        protected override KeywordResult ExecuteKeyword(IWebDriver driver, Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "URL");
            var useBaseUrl = GetOptionalParameter<bool>(parameters, "UseBaseUrl", true);

            try
            {
                string finalUrl = url;
                if (useBaseUrl && !url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    var config = TestConfiguration.Instance;
                    var baseUrl = config.BaseUrl;
                    finalUrl = $"{baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
                }

                driver.Navigate().GoToUrl(finalUrl);
                return KeywordResult.Pass($"Successfully navigated to: {finalUrl}");
            }
            catch (Exception ex)
            {
                return KeywordResult.Fail($"Failed to navigate to URL: {url}", ex);
            }
        }
    }
}

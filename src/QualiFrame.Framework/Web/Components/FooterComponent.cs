using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Generic;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable footer component for web pages.
    /// </summary>
    public class FooterComponent : BaseComponent
    {
        private readonly By _footerLocator;
        private readonly By _socialMediaLinksLocator;
        private readonly By _copyrightTextLocator;

        public FooterComponent(
            IWebDriver driver,
            By footerLocator = null!,
            By socialMediaLinksLocator = null!,
            By copyrightTextLocator = null!)
            : base(driver)
        {
            _footerLocator = footerLocator ?? By.TagName("footer");
            _socialMediaLinksLocator = socialMediaLinksLocator ?? By.CssSelector("footer a");
            _copyrightTextLocator = copyrightTextLocator ?? By.CssSelector("footer .copyright");
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_footerLocator).IsDisplayed();
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
                FindElement(_footerLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Footer component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to wait for footer component visibility");
                throw;
            }
        }

        public string GetCopyrightText()
        {
            try
            {
                return FindElement(_copyrightTextLocator).GetText();
            }
            catch
            {
                return string.Empty;
            }
        }

        public List<string> GetSocialMediaLinks()
        {
            var links = new List<string>();
            try
            {
                var elements = FindElements(_socialMediaLinksLocator);
                foreach (var element in elements)
                {
                    var href = element.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        links.Add(href);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error getting social media links from footer: {Error}", ex.Message);
            }
            return links;
        }

        public void ScrollToFooter()
        {
            ExecuteJavaScript("arguments[0].scrollIntoView(true);", FindElement(_footerLocator).Element);
            Logger.Debug("Scrolled to footer");
        }
    }
}

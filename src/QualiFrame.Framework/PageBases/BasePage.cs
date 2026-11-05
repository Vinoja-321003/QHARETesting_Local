using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using QualiFrame.Framework.Utilities.WebDriver;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Interfaces;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Generic;

namespace QualiFrame.Framework.PageBases
{
    public abstract class BasePage : IPageObject
    {
        protected IWebDriver Driver => EnhancedWebDriverFactory.GetDriver();
        protected WebDriverWait Wait => EnhancedWebDriverFactory.GetWebDriverWait(Driver);
        protected readonly TestConfiguration Config = TestConfiguration.Instance;

        // Element Interaction Methods
        protected IWebElement FindElement(By locator)
        {
            try
            {
                var element = Wait.Until(ExpectedConditions.ElementExists(locator));
                Wait.Until(ExpectedConditions.ElementIsVisible(locator));
                try
                {
                    Wait.Until(ExpectedConditions.ElementToBeClickable(locator));
                }
                catch
                {
                    // Element might not be clickable, that's okay
                }
                return element;
            }
            catch (WebDriverTimeoutException)
            {
                throw new WebDriverTimeoutException(
                    $"Element not found or not accessible: {locator}. " +
                    $"Current URL: {Driver.Url}, Page Title: {Driver.Title}");
            }
        }

        protected IReadOnlyList<IWebElement> FindElements(By locator)
        {
            return Wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(locator));
        }

        protected void ClickElement(By locator)
        {
            var element = FindElement(locator);
            element.Click();
        }

        protected void ClickElement(IWebElement element)
        {
            element.Click();
        }

        protected void SendKeys(By locator, string text)
        {
            try
            {
                WaitForPageLoad();
            }
            catch
            {
                // Page load check might fail, continue anyway
            }
            
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            IWebElement element;
            
            try
            {
                element = wait.Until(ExpectedConditions.ElementExists(locator));
            }
            catch (WebDriverTimeoutException)
            {
                throw new WebDriverTimeoutException(
                    $"Element {locator} not found in DOM. " +
                    $"Current URL: {Driver.Url}, Page Title: {Driver.Title}.");
            }
            
            try
            {
                wait.Until(ExpectedConditions.ElementIsVisible(locator));
            }
            catch (WebDriverTimeoutException)
            {
                throw new WebDriverTimeoutException(
                    $"Element {locator} exists but is not visible. " +
                    $"Current URL: {Driver.Url}, Page Title: {Driver.Title}.");
            }
            
            if (!element.Enabled)
            {
                throw new InvalidOperationException(
                    $"Element {locator} is not enabled and cannot receive input. " +
                    $"Current URL: {Driver.Url}");
            }
            
            element.Clear();
            element.SendKeys(text);
        }

        protected void SendKeys(IWebElement element, string text)
        {
            element.Clear();
            element.SendKeys(text);
        }

        protected string GetText(By locator)
        {
            return FindElement(locator).Text;
        }

        protected string GetText(IWebElement element)
        {
            return element.Text;
        }

        protected string? GetAttribute(By locator, string attributeName)
        {
            return FindElement(locator).GetAttribute(attributeName);
        }

        protected string? GetAttribute(IWebElement element, string attributeName)
        {
            return element.GetAttribute(attributeName);
        }

        protected bool IsElementDisplayed(By locator)
        {
            try
            {
                return FindElement(locator).Displayed;
            }
            catch
            {
                return false;
            }
        }

        protected bool IsElementDisplayed(IWebElement element)
        {
            try
            {
                return element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        protected bool IsElementEnabled(By locator)
        {
            try
            {
                return FindElement(locator).Enabled;
            }
            catch
            {
                return false;
            }
        }

        protected bool IsElementEnabled(IWebElement element)
        {
            try
            {
                return element.Enabled;
            }
            catch
            {
                return false;
            }
        }

        // Wait Methods
        protected void WaitForElementToBeVisible(By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.ElementIsVisible(locator));
        }

        protected void WaitForElementToBeClickable(By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.ElementToBeClickable(locator));
        }

        protected void WaitForElementToDisappear(By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.InvisibilityOfElementLocated(locator));
        }

        protected void WaitForElementToBePresent(By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.ElementExists(locator));
        }

        protected void WaitForTextToBePresentInElement(By locator, string text, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.TextToBePresentInElementLocated(locator, text));
        }

        protected void WaitForUrlToContain(string url, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.UrlContains(url));
        }

        protected void WaitForTitleToContain(string title, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(ExpectedConditions.TitleContains(title));
        }

        // JavaScript Methods
        protected void ScrollToElement(By locator)
        {
            var element = FindElement(locator);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }

        protected void ScrollToElement(IWebElement element)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }

        protected void ScrollToTop()
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("window.scrollTo(0, 0);");
        }

        protected void ScrollToBottom()
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        }

        protected void ExecuteJavaScript(string script, params object[] args)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);
        }

        protected T ExecuteJavaScript<T>(string script, params object[] args)
        {
            var result = ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);
            if (result == null)
            {
                throw new InvalidOperationException($"JavaScript execution returned null for script: {script}");
            }
            return (T)result;
        }

        // Navigation Methods
        protected void NavigateToUrl(string url)
        {
            Driver.Navigate().GoToUrl(url);
        }

        protected void RefreshPage()
        {
            Driver.Navigate().Refresh();
        }

        protected void GoBack()
        {
            Driver.Navigate().Back();
        }

        protected void GoForward()
        {
            Driver.Navigate().Forward();
        }

        protected string GetCurrentUrl()
        {
            return Driver.Url;
        }

        protected string GetPageTitle()
        {
            return Driver.Title;
        }

        // Screenshot Methods
        protected void TakeScreenshot(string fileName)
        {
            try
            {
                var driver = Driver;
                if (driver != null && DriverHealthChecker.IsHealthy(driver))
                {
                    EnhancedWebDriverFactory.TakeScreenshot(driver, fileName);
                }
                else
                {
                    Logger.Warning(
                        "Cannot take screenshot: driver is not available or not healthy.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to take screenshot: {Error}", ex.Message);
            }
        }

        protected byte[] GetScreenshotAsByteArray()
        {
            return ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
        }

        // Window Methods
        protected void SwitchToWindow(string windowHandle)
        {
            Driver.SwitchTo().Window(windowHandle);
        }

        protected IWebDriver closeCurrentDriverMoveToParentWindow(string currentDriverID)
        {
                var allHandles = Driver.WindowHandles;
                // Find and close any window that is NOT the parent
                foreach (var handle in allHandles)
                {
                    if (handle != currentDriverID)
                    {
                        Driver.SwitchTo().Window(handle);
                        Driver.Close();
                        Driver.SwitchTo().Window(currentDriverID);
                       
                    }
                }             
               return Driver;
              
        }


        protected void SwitchToDefaultContent()
        {
            Driver.SwitchTo().DefaultContent();
        }

        protected void SwitchToFrame(int index)
        {
            Driver.SwitchTo().Frame(index);
        }

        protected void SwitchToFrame(string nameOrId)
        {
            Driver.SwitchTo().Frame(nameOrId);
        }

        protected void SwitchToFrame(IWebElement frameElement)
        {
            Driver.SwitchTo().Frame(frameElement);
        }

        protected void SwitchToParentFrame()
        {
            Driver.SwitchTo().ParentFrame();
        }

        // Alert Methods
        protected void AcceptAlert()
        {
            Driver.SwitchTo().Alert().Accept();
        }

        protected void DismissAlert()
        {
            Driver.SwitchTo().Alert().Dismiss();
        }

        protected string GetAlertText()
        {
            var alert = Driver.SwitchTo().Alert();
            return alert.Text ?? string.Empty;
        }

        protected void SendKeysToAlert(string text)
        {
            Driver.SwitchTo().Alert().SendKeys(text);
        }

        // Utility Methods
        protected bool IsElementPresent(By locator)
        {
            try
            {
                Driver.FindElement(locator);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        protected bool IsElementPresent(IWebElement element)
        {
            try
            {
                return element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        protected void WaitForPageLoad()
        {
            Wait.Until(driver =>
            {
                var result = ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState");
                return result != null && result.Equals("complete");
            });
        }

        protected void WaitForAjax()
        {
            Wait.Until(driver =>
            {
                var result = ((IJavaScriptExecutor)driver).ExecuteScript("return jQuery.active == 0");
                return result != null && (bool)result;
            });
        }

        // Abstract methods to be implemented by derived classes
        public abstract void WaitForPageToLoad();
        public abstract bool IsPageLoaded();
    }
}

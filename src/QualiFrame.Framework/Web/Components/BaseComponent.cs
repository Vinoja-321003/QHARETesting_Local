using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using QualiFrame.Framework.Web.Elements;
using System;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Base class for reusable page components.
    /// </summary>
    public abstract class BaseComponent : IComponent
    {
        public IWebDriver Driver { get; }
        protected WebDriverWait Wait { get; }

        protected BaseComponent(IWebDriver driver)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        public abstract bool IsVisible();
        public abstract void WaitForVisible(int timeoutSeconds = 10);

        protected IEnhancedElement FindElement(By locator)
        {
            return ElementActions.FindElement(Driver, locator);
        }

        protected System.Collections.Generic.List<IEnhancedElement> FindElements(By locator)
        {
            return ElementActions.FindElements(Driver, locator);
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

        protected void ExecuteJavaScript(string script, params object[] args)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);
        }
    }
}

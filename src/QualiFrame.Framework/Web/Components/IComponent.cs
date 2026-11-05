using OpenQA.Selenium;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Interface for reusable page components.
    /// </summary>
    public interface IComponent
    {
        IWebDriver Driver { get; }
        bool IsVisible();
        void WaitForVisible(int timeoutSeconds = 10);
    }
}

using System;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Web.Elements
{
    /// <summary>
    /// Interface for enhanced web element operations with fluent API.
    /// </summary>
    public interface IEnhancedElement
    {
        IWebElement Element { get; }
        string Description { get; }
        IEnhancedElement Click(int retryCount = 3);
        IEnhancedElement SendKeys(string text, int retryCount = 3);
        IEnhancedElement Clear();
        string GetText();
        string? GetAttribute(string attributeName);
        bool IsDisplayed();
        bool IsEnabled();
        bool IsSelected();
        IEnhancedElement WaitForVisible(int timeoutSeconds = 10);
        IEnhancedElement WaitForClickable(int timeoutSeconds = 10);
        IEnhancedElement ScrollIntoView();
        IEnhancedElement Highlight();
        IEnhancedElement RemoveHighlight();
        byte[] TakeScreenshot();
        IEnhancedElement JavaScriptClick();
    }
}

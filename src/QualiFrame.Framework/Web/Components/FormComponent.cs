using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable form component for web pages.
    /// </summary>
    public class FormComponent : BaseComponent
    {
        private readonly By _formLocator;
        private readonly By _submitButtonLocator;

        public FormComponent(
            IWebDriver driver,
            By formLocator = null!,
            By submitButtonLocator = null!)
            : base(driver)
        {
            _formLocator = formLocator ?? By.TagName("form");
            _submitButtonLocator = submitButtonLocator ?? By.CssSelector("button[type='submit']");
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_formLocator).IsDisplayed();
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
                FindElement(_formLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Form component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to wait for form component visibility");
                throw;
            }
        }

        public void FillField(string fieldName, string value)
        {
            var fieldLocator = By.Name(fieldName);
            FindElement(fieldLocator).SendKeys(value);
            Logger.Debug("Filled field '{FieldName}' with value", fieldName);
        }

        public void FillFieldById(string fieldId, string value)
        {
            var fieldLocator = By.Id(fieldId);
            FindElement(fieldLocator).SendKeys(value);
            Logger.Debug("Filled field '{FieldId}' with value", fieldId);
        }

        public void ClearField(string fieldName)
        {
            var fieldLocator = By.Name(fieldName);
            FindElement(fieldLocator).Clear();
            Logger.Debug("Cleared field '{FieldName}'", fieldName);
        }

        public void Submit()
        {
            FindElement(_submitButtonLocator).Click();
            Logger.Debug("Submitted form");
        }

        public string GetFieldValue(string fieldName)
        {
            var fieldLocator = By.Name(fieldName);
            return FindElement(fieldLocator).GetAttribute("value") ?? string.Empty;
        }
    }
}

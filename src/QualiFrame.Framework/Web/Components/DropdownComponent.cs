using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable dropdown/select component for web pages.
    /// </summary>
    public class DropdownComponent : BaseComponent
    {
        private readonly By _dropdownLocator;
        private readonly By _optionLocator;
        private readonly By _selectedOptionLocator;

        public DropdownComponent(
            IWebDriver driver,
            By dropdownLocator,
            By optionLocator = null!,
            By selectedOptionLocator = null!)
            : base(driver)
        {
            _dropdownLocator = dropdownLocator ?? throw new ArgumentNullException(nameof(dropdownLocator));
            _optionLocator = optionLocator ?? By.TagName("option");
            _selectedOptionLocator = selectedOptionLocator ?? By.CssSelector("option[selected]");
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_dropdownLocator).IsDisplayed();
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
                FindElement(_dropdownLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Dropdown component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to wait for dropdown component visibility: {Error}", ex.Message);
                throw;
            }
        }

        public void SelectByText(string text)
        {
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                selectElement.SelectByText(text);
                Logger.Debug("Selected dropdown option by text: {Text}", text);
            }
            catch (Exception ex)
            {
                Logger.Warning("Error selecting dropdown option by text '{Text}': {Error}", text, ex.Message);
                throw;
            }
        }

        public void SelectByValue(string value)
        {
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                selectElement.SelectByValue(value);
                Logger.Debug("Selected dropdown option by value: {Value}", value);
            }
            catch (Exception ex)
            {
                Logger.Warning("Error selecting dropdown option by value '{Value}': {Error}", value, ex.Message);
                throw;
            }
        }

        public void SelectByIndex(int index)
        {
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                selectElement.SelectByIndex(index);
                Logger.Debug("Selected dropdown option by index: {Index}", index);
            }
            catch (Exception ex)
            {
                Logger.Warning("Error selecting dropdown option by index '{Index}': {Error}", index, ex.Message);
                throw;
            }
        }

        public List<string> GetOptions()
        {
            var options = new List<string>();
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                options = selectElement.Options.Select(opt => opt.Text).ToList();
            }
            catch (Exception ex)
            {
                Logger.Warning("Error getting dropdown options: {Error}", ex.Message);
            }
            return options;
        }

        public string GetSelectedOption()
        {
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                return selectElement.SelectedOption.Text;
            }
            catch (Exception ex)
            {
                Logger.Warning("Error getting selected option: {Error}", ex.Message);
                return string.Empty;
            }
        }

        public bool IsMultiple()
        {
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                return selectElement.IsMultiple;
            }
            catch
            {
                return false;
            }
        }

        public void DeselectAll()
        {
            try
            {
                var dropdown = FindElement(_dropdownLocator);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown.Element);
                selectElement.DeselectAll();
                Logger.Debug("Deselected all dropdown options");
            }
            catch (Exception ex)
            {
                Logger.Warning("Error deselecting all options: {Error}", ex.Message);
                throw;
            }
        }
    }
}

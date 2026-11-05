using OpenQA.Selenium;
using QualiFrame.Framework.Web.Components;

namespace QualiFrame.Framework.PageBases
{
    /// <summary>
    /// Base page class that supports component-based Page Object Model.
    /// Provides common components like Header, Footer, Navigation, Modal, Table, and Dropdown.
    /// </summary>
    public class ComponentPage : BasePage
    {
        private HeaderComponent? _header;
        private FooterComponent? _footer;
        private NavigationComponent? _navigation;

        /// <summary>
        /// Gets the header component.
        /// </summary>
        public HeaderComponent Header
        {
            get
            {
                if (_header == null)
                {
                    _header = new HeaderComponent(Driver);
                }
                return _header;
            }
        }

        /// <summary>
        /// Gets the footer component.
        /// </summary>
        public FooterComponent Footer
        {
            get
            {
                if (_footer == null)
                {
                    _footer = new FooterComponent(Driver);
                }
                return _footer;
            }
        }

        /// <summary>
        /// Gets the navigation component.
        /// </summary>
        public NavigationComponent Navigation
        {
            get
            {
                if (_navigation == null)
                {
                    _navigation = new NavigationComponent(Driver);
                }
                return _navigation;
            }
        }

        /// <summary>
        /// Creates a form component with custom locators.
        /// </summary>
        protected FormComponent CreateForm(By formLocator, By submitButtonLocator)
        {
            return new FormComponent(Driver, formLocator, submitButtonLocator);
        }

        /// <summary>
        /// Creates a modal component with custom locators.
        /// </summary>
        protected ModalComponent CreateModal(
            By modalLocator,
            By? closeButtonLocator = null,
            By? confirmButtonLocator = null,
            By? cancelButtonLocator = null)
        {
            return new ModalComponent(
                Driver, 
                modalLocator, 
                closeButtonLocator ?? By.ClassName("close"),
                confirmButtonLocator ?? By.CssSelector("button[type='submit']"),
                cancelButtonLocator ?? By.CssSelector("button[type='button']"));
        }

        /// <summary>
        /// Creates a table component with custom locators.
        /// </summary>
        protected TableComponent CreateTable(
            By? tableLocator = null,
            By? headerLocator = null,
            By? rowLocator = null,
            By? cellLocator = null)
        {
            return new TableComponent(
                Driver, 
                tableLocator ?? By.TagName("table"),
                headerLocator ?? By.TagName("th"),
                rowLocator ?? By.TagName("tr"),
                cellLocator ?? By.TagName("td"));
        }

        /// <summary>
        /// Creates a dropdown component with custom locator.
        /// </summary>
        protected DropdownComponent CreateDropdown(By dropdownLocator, By? optionLocator = null)
        {
            return new DropdownComponent(Driver, dropdownLocator, optionLocator ?? By.TagName("option"));
        }

        /// <summary>
        /// Waits for the page to load.
        /// Default implementation waits for header component.
        /// </summary>
        public override void WaitForPageToLoad()
        {
            try
            {
                Header.WaitForVisible();
            }
            catch
            {
                WaitForPageLoad();
            }
        }

        /// <summary>
        /// Checks if the page is loaded.
        /// Default implementation checks if header component is visible.
        /// </summary>
        public override bool IsPageLoaded()
        {
            try
            {
                return Header.IsVisible();
            }
            catch
            {
                return true;
            }
        }
    }
}

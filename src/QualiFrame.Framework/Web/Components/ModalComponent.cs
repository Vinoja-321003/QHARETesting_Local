using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable modal/dialog component for web pages.
    /// </summary>
    public class ModalComponent : BaseComponent
    {
        private readonly By _modalLocator;
        private readonly By _closeButtonLocator;
        private readonly By _confirmButtonLocator;
        private readonly By _cancelButtonLocator;
        private readonly By _titleLocator;
        private readonly By _bodyLocator;

        public ModalComponent(
            IWebDriver driver,
            By modalLocator = null!,
            By closeButtonLocator = null!,
            By confirmButtonLocator = null!,
            By cancelButtonLocator = null!,
            By titleLocator = null!,
            By bodyLocator = null!)
            : base(driver)
        {
            _modalLocator = modalLocator ?? By.ClassName("modal");
            _closeButtonLocator = closeButtonLocator ?? By.ClassName("close");
            _confirmButtonLocator = confirmButtonLocator ?? By.CssSelector("button[type='submit']");
            _cancelButtonLocator = cancelButtonLocator ?? By.CssSelector("button[type='button']");
            _titleLocator = titleLocator ?? By.ClassName("modal-title");
            _bodyLocator = bodyLocator ?? By.ClassName("modal-body");
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_modalLocator).IsDisplayed();
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
                FindElement(_modalLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Modal component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to wait for modal component visibility: {Error}", ex.Message);
                throw;
            }
        }

        public void Close()
        {
            try
            {
                FindElement(_closeButtonLocator).Click();
                Logger.Debug("Closed modal using close button");
            }
            catch (Exception ex)
            {
                Logger.Warning("Error closing modal: {Error}", ex.Message);
                throw;
            }
        }

        public void Confirm()
        {
            try
            {
                FindElement(_confirmButtonLocator).Click();
                Logger.Debug("Confirmed modal action");
            }
            catch (Exception ex)
            {
                Logger.Warning("Error confirming modal: {Error}", ex.Message);
                throw;
            }
        }

        public void Cancel()
        {
            try
            {
                FindElement(_cancelButtonLocator).Click();
                Logger.Debug("Cancelled modal action");
            }
            catch (Exception ex)
            {
                Logger.Warning("Error cancelling modal: {Error}", ex.Message);
                throw;
            }
        }

        public string GetTitle()
        {
            try
            {
                return FindElement(_titleLocator).GetText();
            }
            catch
            {
                return string.Empty;
            }
        }

        public string GetBodyText()
        {
            try
            {
                return FindElement(_bodyLocator).GetText();
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool IsClosed()
        {
            return !IsVisible();
        }
    }
}

using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Mobile.Utilities;

namespace QualiFrame.Framework.Mobile.Elements
{
    /// <summary>
    /// Enhanced mobile element wrapper with mobile-specific operations.
    /// </summary>
    public class MobileElement
    {
        private readonly AppiumDriver _driver;
        private readonly By _locator;
        private IWebElement? _element;
        private readonly string _description;

        public IWebElement Element
        {
            get
            {
                if (_element == null || !IsElementValid(_element))
                {
                    RefreshElement();
                }
                return _element!;
            }
        }

        public string Description => _description;

        public MobileElement(AppiumDriver driver, By locator, string description = "")
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _description = string.IsNullOrEmpty(description) ? locator.ToString() : description;
            RefreshElement();
        }

        public MobileElement(AppiumDriver driver, IWebElement element, string description = "")
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _locator = By.Id("");
            _description = string.IsNullOrEmpty(description) ? "MobileElement" : description;
        }

        private void RefreshElement()
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                _element = wait.Until(d => d.FindElement(_locator));
                Logger.Debug("Mobile element refreshed: {Description}", Description);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to refresh mobile element: {Description}", Description);
                throw new QualiFrame.Framework.Core.Exceptions.WebDriverException(
                    $"Mobile element not found: {Description}",
                    browser: _driver.GetType().Name,
                    testId: Logger.TestId);
            }
        }

        private bool IsElementValid(IWebElement element)
        {
            try
            {
                _ = element.Displayed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Taps the element.
        /// </summary>
        public MobileElement Tap()
        {
            Logger.Debug("Tapping mobile element: {Description}", Description);
            GestureActions.Tap(_driver, Element);
            return this;
        }

        /// <summary>
        /// Double taps the element.
        /// </summary>
        public MobileElement DoubleTap()
        {
            Logger.Debug("Double tapping mobile element: {Description}", Description);
            GestureActions.DoubleTap(_driver, Element);
            return this;
        }

        /// <summary>
        /// Long presses the element.
        /// </summary>
        public MobileElement LongPress(int duration = 2000)
        {
            Logger.Debug("Long pressing mobile element: {Description}", Description);
            GestureActions.LongPress(_driver, Element, duration);
            return this;
        }

        /// <summary>
        /// Sends keys to the element.
        /// </summary>
        public MobileElement SendKeys(string text)
        {
            Logger.Debug("Sending keys to mobile element: {Description}, Text length: {Length}", Description, text.Length);
            Element.Clear();
            Element.SendKeys(text);
            return this;
        }

        /// <summary>
        /// Gets the text of the element.
        /// </summary>
        public string GetText()
        {
            try
            {
                var text = Element.Text;
                Logger.Debug("Retrieved text from mobile element: {Description}, Text: {Text}", Description, text);
                return text;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get text from mobile element: {Description}", Description);
                throw;
            }
        }

        /// <summary>
        /// Checks if element is displayed.
        /// </summary>
        public bool IsDisplayed()
        {
            try
            {
                return Element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Swipes the element in a direction.
        /// </summary>
        public MobileElement Swipe(string direction, int distance = 500)
        {
            var location = Element.Location;
            var size = Element.Size;
            var centerX = location.X + size.Width / 2;
            var centerY = location.Y + size.Height / 2;

            Logger.Debug("Swiping mobile element {Description} {Direction}", Description, direction);

            switch (direction.ToLower())
            {
                case "up":
                    GestureActions.Swipe(_driver, centerX, centerY, centerX, centerY - distance);
                    break;
                case "down":
                    GestureActions.Swipe(_driver, centerX, centerY, centerX, centerY + distance);
                    break;
                case "left":
                    GestureActions.Swipe(_driver, centerX, centerY, centerX - distance, centerY);
                    break;
                case "right":
                    GestureActions.Swipe(_driver, centerX, centerY, centerX + distance, centerY);
                    break;
                default:
                    Logger.Warning("Unknown swipe direction: {Direction}", direction);
                    break;
            }

            return this;
        }
    }
}

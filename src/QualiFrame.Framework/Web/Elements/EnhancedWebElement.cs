using System;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;
using Polly;
using SkiaSharp;
using WebDriverException = QualiFrame.Framework.Core.Exceptions.WebDriverException;

namespace QualiFrame.Framework.Web.Elements
{
    /// <summary>
    /// Enhanced web element wrapper with fluent interface, retry mechanism, and debugging features.
    /// </summary>
    public class EnhancedWebElement : IEnhancedElement
    {
        private readonly IWebDriver _driver;
        private readonly By _locator;
        private IWebElement? _element;
        private bool _isHighlighted;

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

        public string Description { get; }

        public EnhancedWebElement(IWebDriver driver, By locator, string description = "")
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            Description = string.IsNullOrEmpty(description) ? locator.ToString() : description;
            RefreshElement();
        }

        public EnhancedWebElement(IWebDriver driver, IWebElement element, string description = "")
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _locator = By.Id("");
            Description = string.IsNullOrEmpty(description) ? "WebElement" : description;
        }

        private void RefreshElement()
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                _element = wait.Until(ExpectedConditions.ElementExists(_locator));
                Logger.Debug("Element refreshed: {Description}", Description);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to refresh element: {Description}", Description);
                throw new WebDriverException(
                    $"Element not found: {Description}",
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

        public IEnhancedElement Click(int retryCount = 3)
        {
            Logger.Debug("Clicking element: {Description}", Description);

            var retryPolicy = Policy
                .Handle<WebDriverException>()
                .Or<StaleElementReferenceException>()
                .WaitAndRetry(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(0.5 * retryAttempt),
                    onRetry: (exception, timeSpan, count, context) =>
                    {
                        Logger.Warning("Click retry {Count}/{MaxRetries} for {Description}: {Message}",
                            count, retryCount, Description, exception.Message);
                        RefreshElement();
                    });

            retryPolicy.Execute(() =>
            {
                WaitForClickable();
                Highlight();
                Element.Click();
                RemoveHighlight();
                Logger.Debug("Successfully clicked element: {Description}", Description);
            });

            return this;
        }

        public IEnhancedElement SendKeys(string text, int retryCount = 3)
        {
            if (string.IsNullOrEmpty(text))
            {
                Logger.Warning("Attempted to send empty text to element: {Description}", Description);
                return this;
            }

            Logger.Debug("Sending keys to element: {Description}, Text length: {Length}", Description, text.Length);

            var retryPolicy = Policy
                .Handle<WebDriverException>()
                .Or<StaleElementReferenceException>()
                .WaitAndRetry(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(0.5 * retryAttempt),
                    onRetry: (exception, timeSpan, count, context) =>
                    {
                        Logger.Warning("SendKeys retry {Count}/{MaxRetries} for {Description}: {Message}",
                            count, retryCount, Description, exception.Message);
                        RefreshElement();
                    });

            retryPolicy.Execute(() =>
            {
                WaitForVisible();
                Highlight();
                Element.Clear();
                Element.SendKeys(text);
                RemoveHighlight();
                Logger.Debug("Successfully sent keys to element: {Description}", Description);
            });

            return this;
        }

        public IEnhancedElement Clear()
        {
            Logger.Debug("Clearing element: {Description}", Description);
            try
            {
                Element.Clear();
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to clear element: {Description}, Error: {Error}", Description, ex.Message);
                RefreshElement();
                Element.Clear();
            }
            return this;
        }

        public string GetText()
        {
            try
            {
                var text = Element.Text;
                Logger.Debug("Retrieved text from element: {Description}, Text: {Text}", Description, text);
                return text;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get text from element: {Description}", Description);
                throw;
            }
        }

        public string? GetAttribute(string attributeName)
        {
            try
            {
                var value = Element.GetAttribute(attributeName);
                Logger.Debug("Retrieved attribute '{Attribute}' from element: {Description}, Value: {Value}",
                    attributeName, Description, value ?? "null");
                return value;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get attribute '{Attribute}' from element: {Description}",
                    attributeName, Description);
                throw;
            }
        }

        public bool IsDisplayed()
        {
            try
            {
                var displayed = Element.Displayed;
                Logger.Debug("Element display status: {Description}, Displayed: {Displayed}", Description, displayed);
                return displayed;
            }
            catch
            {
                return false;
            }
        }

        public bool IsEnabled()
        {
            try
            {
                var enabled = Element.Enabled;
                Logger.Debug("Element enabled status: {Description}, Enabled: {Enabled}", Description, enabled);
                return enabled;
            }
            catch
            {
                return false;
            }
        }

        public bool IsSelected()
        {
            try
            {
                var selected = Element.Selected;
                Logger.Debug("Element selected status: {Description}, Selected: {Selected}", Description, selected);
                return selected;
            }
            catch
            {
                return false;
            }
        }

        public IEnhancedElement WaitForVisible(int timeoutSeconds = 10)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(ExpectedConditions.ElementIsVisible(_locator));
                Logger.Debug("Element is visible: {Description}", Description);
            }
            catch (Exception ex)
            {
                Logger.Warning("Element not visible within {Timeout}s: {Description}, Error: {Error}", timeoutSeconds, Description, ex.Message);
                throw new WebDriverException(
                    $"Element not visible: {Description}",
                    browser: _driver.GetType().Name,
                    testId: Logger.TestId);
            }
            return this;
        }

        public IEnhancedElement WaitForClickable(int timeoutSeconds = 10)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(ExpectedConditions.ElementToBeClickable(_locator));
                Logger.Debug("Element is clickable: {Description}", Description);
            }
            catch (Exception ex)
            {
                Logger.Warning("Element not clickable within {Timeout}s: {Description}, Error: {Error}", timeoutSeconds, Description, ex.Message);
                throw new WebDriverException(
                    $"Element not clickable: {Description}",
                    browser: _driver.GetType().Name,
                    testId: Logger.TestId);
            }
            return this;
        }

        public IEnhancedElement ScrollIntoView()
        {
            try
            {
                var jsExecutor = (IJavaScriptExecutor)_driver;
                jsExecutor.ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", Element);
                Logger.Debug("Scrolled to element: {Description}", Description);
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to scroll to element: {Description}, Error: {Error}", Description, ex.Message);
            }
            return this;
        }

        public IEnhancedElement Highlight()
        {
            if (_isHighlighted)
                return this;

            try
            {
                var jsExecutor = (IJavaScriptExecutor)_driver;
                var originalStyle = Element.GetAttribute("style");
                jsExecutor.ExecuteScript(
                    "arguments[0].setAttribute('style', arguments[1]);",
                    Element,
                    "border: 3px solid red; background-color: yellow;");
                
                _isHighlighted = true;
                Logger.Debug("Element highlighted: {Description}", Description);
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to highlight element: {Description}, Error: {Error}", Description, ex.Message);
            }
            return this;
        }

        public IEnhancedElement RemoveHighlight()
        {
            if (!_isHighlighted)
                return this;

            try
            {
                var jsExecutor = (IJavaScriptExecutor)_driver;
                jsExecutor.ExecuteScript("arguments[0].removeAttribute('style');", Element);
                _isHighlighted = false;
                Logger.Debug("Element highlight removed: {Description}", Description);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to remove highlight from element: {Description}, Error: {Error}", Description, ex.Message);
            }
            return this;
        }

        public byte[] TakeScreenshot()
        {
            try
            {
                var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                var elementScreenshot = CropElementFromScreenshot(screenshot);
                Logger.Debug("Screenshot taken for element: {Description}", Description);
                return elementScreenshot;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to take screenshot of element: {Description}", Description);
                throw;
            }
        }

        private byte[] CropElementFromScreenshot(Screenshot screenshot)
        {
            try
            {
                var elementLocation = Element.Location;
                var elementSize = Element.Size;
                using var bitmap = SKBitmap.Decode(screenshot.AsByteArray);
                if (bitmap == null)
                {
                    throw new InvalidOperationException("Unable to decode screenshot image.");
                }

                var x = elementLocation.X;
                var y = elementLocation.Y;
                var width = Math.Min(elementSize.Width, bitmap.Width - x);
                var height = Math.Min(elementSize.Height, bitmap.Height - y);

                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x + width > bitmap.Width) width = bitmap.Width - x;
                if (y + height > bitmap.Height) height = bitmap.Height - y;

                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Calculated crop rectangle is invalid.");
                }

                using var cropped = new SKBitmap(width, height);
                using (var canvas = new SKCanvas(cropped))
                {
                    var srcRect = new SKRectI(x, y, x + width, y + height);
                    var destRect = new SKRectI(0, 0, width, height);
                    canvas.DrawBitmap(bitmap, srcRect, destRect);
                }

                using var image = SKImage.FromBitmap(cropped);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to crop element from screenshot: {Error}. Returning full screenshot.", ex.Message);
                return screenshot.AsByteArray;
            }
        }

        public IEnhancedElement JavaScriptClick()
        {
            try
            {
                var jsExecutor = (IJavaScriptExecutor)_driver;
                jsExecutor.ExecuteScript("arguments[0].click();", Element);
                Logger.Debug("JavaScript click performed on element: {Description}", Description);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform JavaScript click on element: {Description}", Description);
                throw;
            }
            return this;
        }
    }
}

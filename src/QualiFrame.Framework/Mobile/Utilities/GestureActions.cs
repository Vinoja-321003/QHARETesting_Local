using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Mobile.Utilities
{
    /// <summary>
    /// Mobile gesture actions for Appium using W3C Actions API.
    /// </summary>
    public static class GestureActions
    {
        /// <summary>
        /// Performs a swipe gesture using W3C Actions API.
        /// </summary>
        public static void Swipe(AppiumDriver driver, int startX, int startY, int endX, int endY, int duration = 1000)
        {
            try
            {
                var actions = new Actions(driver);
                var offsetX = endX - startX;
                var offsetY = endY - startY;
                
                actions
                    .MoveToLocation(startX, startY)
                    .ClickAndHold()
                    .MoveByOffset(offsetX, offsetY)
                    .Pause(TimeSpan.FromMilliseconds(duration))
                    .Release()
                    .Perform();

                Logger.Debug("Swipe performed: ({StartX},{StartY}) -> ({EndX},{EndY})", startX, startY, endX, endY);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform swipe gesture: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs a swipe up gesture.
        /// </summary>
        public static void SwipeUp(AppiumDriver driver, int startY = 500, int distance = 500)
        {
            var size = driver.Manage().Window.Size;
            Swipe(driver, size.Width / 2, startY, size.Width / 2, startY - distance);
            Logger.Debug("Swipe up performed");
        }

        /// <summary>
        /// Performs a swipe down gesture.
        /// </summary>
        public static void SwipeDown(AppiumDriver driver, int startY = 500, int distance = 500)
        {
            var size = driver.Manage().Window.Size;
            Swipe(driver, size.Width / 2, startY, size.Width / 2, startY + distance);
            Logger.Debug("Swipe down performed");
        }

        /// <summary>
        /// Performs a swipe left gesture.
        /// </summary>
        public static void SwipeLeft(AppiumDriver driver, int startX = 500, int distance = 500)
        {
            var size = driver.Manage().Window.Size;
            Swipe(driver, startX, size.Height / 2, startX - distance, size.Height / 2);
            Logger.Debug("Swipe left performed");
        }

        /// <summary>
        /// Performs a swipe right gesture.
        /// </summary>
        public static void SwipeRight(AppiumDriver driver, int startX = 500, int distance = 500)
        {
            var size = driver.Manage().Window.Size;
            Swipe(driver, startX, size.Height / 2, startX + distance, size.Height / 2);
            Logger.Debug("Swipe right performed");
        }

        /// <summary>
        /// Performs a pinch gesture (zoom out) using W3C Actions API.
        /// </summary>
        public static void Pinch(AppiumDriver driver, IWebElement element, double scale = 0.5)
        {
            try
            {
                var location = element.Location;
                var size = element.Size;
                var centerX = location.X + size.Width / 2;
                var centerY = location.Y + size.Height / 2;

                var startDistance = Math.Min(size.Width, size.Height) / 2;
                var endDistance = (int)(startDistance * scale);
                var offsetX = -(endDistance - startDistance);

                var actions = new Actions(driver);
                actions
                    .MoveToLocation(centerX, centerY)
                    .ClickAndHold()
                    .MoveByOffset(offsetX, 0)
                    .Release()
                    .Perform();

                Logger.Debug("Pinch gesture performed on element");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform pinch gesture: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs a zoom gesture (zoom in) using W3C Actions API.
        /// </summary>
        public static void Zoom(AppiumDriver driver, IWebElement element, double scale = 2.0)
        {
            try
            {
                var location = element.Location;
                var size = element.Size;
                var centerX = location.X + size.Width / 2;
                var centerY = location.Y + size.Height / 2;

                var startDistance = Math.Min(size.Width, size.Height) / 4;
                var endDistance = (int)(startDistance * scale);
                var offsetX = endDistance - startDistance;

                var actions = new Actions(driver);
                actions
                    .MoveToLocation(centerX, centerY)
                    .ClickAndHold()
                    .MoveByOffset(offsetX, 0)
                    .Release()
                    .Perform();

                Logger.Debug("Zoom gesture performed on element");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform zoom gesture: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs a long press gesture using W3C Actions API.
        /// </summary>
        public static void LongPress(AppiumDriver driver, IWebElement element, int duration = 2000)
        {
            try
            {
                var actions = new Actions(driver);
                actions
                    .MoveToElement(element)
                    .ClickAndHold()
                    .Pause(TimeSpan.FromMilliseconds(duration))
                    .Release()
                    .Perform();

                Logger.Debug("Long press performed on element for {Duration}ms", duration);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform long press: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs a tap gesture using W3C Actions API.
        /// </summary>
        public static void Tap(AppiumDriver driver, IWebElement element)
        {
            try
            {
                var actions = new Actions(driver);
                actions
                    .MoveToElement(element)
                    .Click()
                    .Perform();
                Logger.Debug("Tap performed on element");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform tap: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs a tap at coordinates using W3C Actions API.
        /// </summary>
        public static void Tap(AppiumDriver driver, int x, int y)
        {
            try
            {
                var actions = new Actions(driver);
                actions
                    .MoveToLocation(x, y)
                    .Click()
                    .Perform();
                Logger.Debug("Tap performed at coordinates ({X},{Y})", x, y);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform tap at coordinates: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs a double tap gesture using W3C Actions API.
        /// </summary>
        public static void DoubleTap(AppiumDriver driver, IWebElement element)
        {
            try
            {
                var actions = new Actions(driver);
                actions
                    .MoveToElement(element)
                    .Click()
                    .Pause(TimeSpan.FromMilliseconds(100))
                    .Click()
                    .Perform();
                Logger.Debug("Double tap performed on element");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to perform double tap: {Error}", ex.Message);
                throw;
            }
        }
    }
}

using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Mobile.Utilities
{
    /// <summary>
    /// Utility for handling mobile keyboard operations.
    /// </summary>
    public static class KeyboardHandler
    {
        /// <summary>
        /// Hides the keyboard (Android).
        /// </summary>
        public static void HideKeyboard(AppiumDriver driver)
        {
            try
            {
                if (driver is AndroidDriver androidDriver)
                {
                    androidDriver.HideKeyboard();
                    Logger.Debug("Keyboard hidden (Android)");
                }
                else if (driver is IOSDriver iosDriver)
                {
                    iosDriver.HideKeyboard("Done");
                    Logger.Debug("Keyboard hidden (iOS)");
                }
                else
                {
                    Logger.Warning("Keyboard hide not supported for driver type: {Type}", driver.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to hide keyboard: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Hides the keyboard (iOS with specific key).
        /// </summary>
        public static void HideKeyboard(AppiumDriver driver, string keyName)
        {
            try
            {
                if (driver is IOSDriver iosDriver)
                {
                    iosDriver.HideKeyboard(keyName);
                    Logger.Debug("Keyboard hidden (iOS) with key: {Key}", keyName);
                }
                else
                {
                    HideKeyboard(driver);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to hide keyboard: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Checks if keyboard is displayed.
        /// </summary>
        public static bool IsKeyboardDisplayed(AppiumDriver driver)
        {
            try
            {
                var keyboardElements = driver.FindElements(By.ClassName("XCUIElementTypeKeyboard"));
                if (keyboardElements.Count > 0)
                {
                    return true;
                }

                var androidKeyboard = driver.FindElements(By.ClassName("android.widget.EditText"));
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Presses a key on the keyboard.
        /// </summary>
        public static void PressKey(AppiumDriver driver, string key)
        {
            try
            {
                driver.SwitchTo().ActiveElement().SendKeys(key);
                Logger.Debug("Key pressed: {Key}", key);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to press key: {Error}", ex.Message);
                throw;
            }
        }
    }
}

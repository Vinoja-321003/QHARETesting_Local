using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Mobile.Utilities
{
    /// <summary>
    /// Utility for rotating mobile device orientation.
    /// </summary>
    public static class DeviceRotator
    {
        /// <summary>
        /// Rotates device to landscape orientation.
        /// </summary>
        public static void RotateToLandscape(AppiumDriver driver)
        {
            try
            {
                driver.Orientation = ScreenOrientation.Landscape;
                Logger.Information("Device rotated to landscape orientation");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to rotate device to landscape: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Rotates device to portrait orientation.
        /// </summary>
        public static void RotateToPortrait(AppiumDriver driver)
        {
            try
            {
                driver.Orientation = ScreenOrientation.Portrait;
                Logger.Information("Device rotated to portrait orientation");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to rotate device to portrait: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets current device orientation.
        /// </summary>
        public static string GetOrientation(AppiumDriver driver)
        {
            try
            {
                return driver.Orientation.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get device orientation: {Error}", ex.Message);
                throw;
            }
        }
    }
}

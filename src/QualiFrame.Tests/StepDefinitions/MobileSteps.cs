using System;
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Mobile.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Mobile;
using QualiFrame.Tests.PageObjects;
using Reqnroll;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    public class MobileSteps
    {
        private readonly MobileAppScreen _screen = new();
        private static readonly TestConfiguration Config = TestConfiguration.Instance;

        private static AppiumDriver Driver => AppiumDriverFactory.GetDriver();

        [Given(@"I have a mobile device connected")]
        public void GivenIHaveAMobileDeviceConnected()
        {
            var serverUrl = Config.AppiumServerUrl;
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new InvalidOperationException("Set Mobile:AppiumServerUrl (e.g., http://localhost:4723).");
            }

            AppiumServerManager.StartAppiumServerIfNeeded();
            if (!AppiumServerManager.IsAppiumServerRunning(serverUrl))
            {
                Assert.Inconclusive($"Cannot connect to Appium server at {serverUrl}. Start Appium or set Mobile:AutoStartAppium.");
            }

            ExtentReportManager.LogPass($"Appium server is accessible at {serverUrl}");
        }

        [Given(@"I launch the mobile application")]
        [When(@"I launch the mobile application")]
        public void LaunchTheMobileApplication()
        {
            try
            {
                AppiumDriverFactory.CreateDriver();
                ExtentReportManager.LogPass($"Mobile application launched ({Config.PlatformName} / {Config.DeviceName})");
            }
            catch (Exception ex) when (IsConnectionFailure(ex))
            {
                Assert.Inconclusive($"Cannot create an Appium session at {Config.AppiumServerUrl}: {ex.Message}");
            }
        }

        [When(@"I tap on the element with id ""(.*)""")]
        public void WhenITapOnTheElementWithId(string elementId)
        {
            _screen.ElementById(elementId).Tap();
            ExtentReportManager.LogPass($"Tapped on element with id: {elementId}");
        }

        [When(@"I tap on element with xpath ""(.*)""")]
        public void WhenITapOnElementWithXPath(string xpath)
        {
            new QualiFrame.Framework.Mobile.Elements.MobileElement(Driver, OpenQA.Selenium.By.XPath(xpath), xpath).Tap();
            ExtentReportManager.LogPass($"Tapped on element with xpath: {xpath}");
        }

        [When(@"I enter ""(.*)"" in the field with id ""(.*)""")]
        public void WhenIEnterInTheFieldWithId(string text, string elementId)
        {
            _screen.ElementById(elementId).SendKeys(text);
            ExtentReportManager.LogPass($"Entered text in field with id: {elementId}");
        }

        [When(@"I enter ""(.*)"" in field with xpath ""(.*)""")]
        public void WhenIEnterInFieldWithXPath(string text, string xpath)
        {
            new QualiFrame.Framework.Mobile.Elements.MobileElement(Driver, OpenQA.Selenium.By.XPath(xpath), xpath).SendKeys(text);
            ExtentReportManager.LogPass($"Entered text in field with xpath: {xpath}");
        }

        [When(@"I swipe from ""(.*)"" to ""(.*)""")]
        public void WhenISwipeFromTo(string startCoordinates, string endCoordinates)
        {
            var startParts = startCoordinates.Split(',');
            var endParts = endCoordinates.Split(',');
            GestureActions.Swipe(
                Driver,
                int.Parse(startParts[0]),
                int.Parse(startParts[1]),
                int.Parse(endParts[0]),
                int.Parse(endParts[1]));
            ExtentReportManager.LogPass($"Swiped from {startCoordinates} to {endCoordinates}");
        }

        [When(@"I swipe up")]
        public void WhenISwipeUp()
        {
            GestureActions.SwipeUp(Driver);
            ExtentReportManager.LogPass("Swiped up");
        }

        [When(@"I swipe down")]
        public void WhenISwipeDown()
        {
            GestureActions.SwipeDown(Driver);
            ExtentReportManager.LogPass("Swiped down");
        }

        [When(@"I rotate the device to landscape")]
        public void WhenIRotateTheDeviceToLandscape()
        {
            DeviceRotator.RotateToLandscape(Driver);
            ExtentReportManager.LogPass("Rotated to landscape");
        }

        [When(@"I rotate the device to portrait")]
        public void WhenIRotateTheDeviceToPortrait()
        {
            DeviceRotator.RotateToPortrait(Driver);
            ExtentReportManager.LogPass("Rotated to portrait");
        }

        [Then(@"the keyboard should be visible")]
        public void ThenTheKeyboardShouldBeVisible()
        {
            Assert.That(KeyboardHandler.IsKeyboardDisplayed(Driver), Is.True, "Keyboard was not visible.");
        }

        [When(@"I hide the keyboard")]
        public void WhenIHideTheKeyboard()
        {
            KeyboardHandler.HideKeyboard(Driver);
            ExtentReportManager.LogPass("Hid keyboard");
        }

        [Then(@"the keyboard should not be visible")]
        public void ThenTheKeyboardShouldNotBeVisible()
        {
            Assert.That(KeyboardHandler.IsKeyboardDisplayed(Driver), Is.False, "Keyboard was still visible.");
        }

        [When(@"I press the back button")]
        public void WhenIPressTheBackButton()
        {
            Driver.Navigate().Back();
            ExtentReportManager.LogPass("Pressed back");
        }

        [When(@"I press the home button")]
        public void WhenIPressTheHomeButton()
        {
            Driver.Navigate().Back();
            ExtentReportManager.LogPass("Pressed home (navigate back fallback)");
        }

        [When(@"I switch to the mobile webview")]
        public void WhenISwitchToTheMobileWebview()
        {
            if (!MobileContextSwitcher.HasWebView(Driver))
            {
                Assert.Inconclusive("No WEBVIEW context is available on this session.");
            }

            MobileContextSwitcher.SwitchToWebView(Driver);
            ExtentReportManager.LogPass("Switched to WEBVIEW");
        }

        [When(@"I switch to the native mobile context")]
        public void WhenISwitchToTheNativeMobileContext()
        {
            MobileContextSwitcher.SwitchToNative(Driver);
            ExtentReportManager.LogPass("Switched to NATIVE_APP");
        }

        [Then(@"I should see the element with id ""(.*)""")]
        public void ThenIShouldSeeTheElementWithId(string elementId)
        {
            Assert.That(_screen.IsVisible(elementId), Is.True, $"Element with id '{elementId}' is not visible");
            ExtentReportManager.LogPass($"Element with id '{elementId}' is visible");
        }

        [Then(@"I should see element with xpath ""(.*)""")]
        public void ThenIShouldSeeElementWithXPath(string xpath)
        {
            var element = new QualiFrame.Framework.Mobile.Elements.MobileElement(Driver, OpenQA.Selenium.By.XPath(xpath), xpath);
            Assert.That(element.IsDisplayed(), Is.True, $"Element with xpath '{xpath}' is not visible");
            ExtentReportManager.LogPass($"Element with xpath '{xpath}' is visible");
        }

        [When(@"I close the mobile application")]
        public void WhenICloseTheMobileApplication()
        {
            AppiumDriverFactory.QuitDriver();
            ExtentReportManager.LogPass("Mobile application closed successfully");
        }

        private static bool IsConnectionFailure(Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            return message.Contains("refused", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("No connection", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("not reachable", StringComparison.OrdinalIgnoreCase);
        }
    }
}

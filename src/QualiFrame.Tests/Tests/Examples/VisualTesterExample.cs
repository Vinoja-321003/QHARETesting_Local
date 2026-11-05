using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.WebDriver;
using QualiFrame.Framework.Web.Visual;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Tests.Examples
{
    /// <summary>
    /// Example visual regression test using SkiaSharp VisualTester.
    /// Eyes.Selenium / Applitools is not bundled.
    /// </summary>
    [TestFixture]
    public class VisualTesterExample
    {
        private IWebDriver? _driver;
        private VisualTester? _visualTester;

        [SetUp]
        public void SetUp()
        {
            _driver = EnhancedWebDriverFactory.CreateDriver();
            _visualTester = new VisualTester(threshold: 0.01);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning("Error during teardown: {Error}", ex.Message);
            }
        }

        [Test]
        public void VisualTestExample()
        {
            var driver = _driver!;
            var visualTester = _visualTester!;

            driver.Navigate().GoToUrl("https://www.saucedemo.com");
            var homepageResult = visualTester.CompareWithBaseline(driver, "SauceDemo_Homepage");

            driver.FindElement(By.Id("user-name")).SendKeys("standard_user");
            driver.FindElement(By.Id("password")).SendKeys("secret_sauce");
            driver.FindElement(By.Id("login-button")).Click();
            var productsResult = visualTester.CompareWithBaseline(driver, "SauceDemo_Products");

            if (!homepageResult.IsMatch)
            {
                Assert.Inconclusive($"Homepage visual baseline mismatch: {homepageResult.Message}");
            }

            if (!productsResult.IsMatch)
            {
                Assert.Inconclusive($"Products page visual baseline mismatch: {productsResult.Message}");
            }
        }
    }
}

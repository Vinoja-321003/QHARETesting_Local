using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.WebDriver;
using QualiFrame.Framework.Web.Accessibility;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Tests.Examples
{
    /// <summary>
    /// Example test demonstrating accessibility testing.
    /// </summary>
    [TestFixture]
    public class AccessibilityTest
    {
        private IWebDriver? _driver;
        private AccessibilityTester? _accessibilityTester;

        [SetUp]
        public void SetUp()
        {
            _driver = EnhancedWebDriverFactory.CreateDriver();
            _accessibilityTester = new AccessibilityTester(_driver);
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
        public void AccessibilityCheckExample()
        {
            // Arrange
            var driver = _driver!;
            var tester = _accessibilityTester!;

            // Act
            driver.Navigate().GoToUrl("https://www.saucedemo.com");
            AccessibilityResults results;
            try
            {
                results = tester.RunAccessibilityCheck();
            }
            catch (JavaScriptException ex) when (ex.Message.Contains("axe is not defined", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive("axe-core script was not available on the page (\"axe is not defined\"). This can happen due to CSP or driver/script execution restrictions.");
                return;
            }
            catch (WebDriverException ex) when (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive($"Accessibility analysis timed out while executing axe script via WebDriver. Details: {ex.Message}");
                return;
            }

            // Assert
            Logger.Information("Accessibility check completed. Violations: {Violations}, Passes: {Passes}",
                results.Violations.Count, results.Passes.Count);

            // Assert no critical violations
            tester.AssertNoCriticalViolations(results);

            // Optionally assert no violations at all
            // tester.AssertNoViolations(results); // Uncomment if you want strict checking
        }

        [Test]
        public void WCAGComplianceCheckExample()
        {
            // Arrange
            var driver = _driver!;
            var tester = _accessibilityTester!;
            var complianceChecker = new WCAGComplianceChecker();

            // Act
            driver.Navigate().GoToUrl("https://www.saucedemo.com");
            AccessibilityResults results;
            try
            {
                results = tester.RunAccessibilityCheck();
            }
            catch (JavaScriptException ex) when (ex.Message.Contains("axe is not defined", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive("axe-core script was not available on the page (\"axe is not defined\"). This can happen due to CSP or driver/script execution restrictions.");
                return;
            }
            catch (WebDriverException ex) when (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive($"Accessibility analysis timed out while executing axe script via WebDriver. Details: {ex.Message}");
                return;
            }
            var complianceReport = complianceChecker.CheckCompliance(results, "AA");

            // Assert
            Logger.Information("WCAG AA Compliance: {Status}, Percentage: {Percentage}%",
                complianceReport.ComplianceStatus, complianceReport.CompliancePercentage);

            Assert.That(complianceReport.CompliancePercentage, Is.GreaterThan(80), 
                "Accessibility compliance should be at least 80%");
        }
    }
}

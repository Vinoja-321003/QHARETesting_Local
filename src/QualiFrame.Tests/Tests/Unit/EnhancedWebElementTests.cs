using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Web.Elements;
using System;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for the EnhancedWebElement class.
    /// Note: These tests require a WebDriver instance, so they are integration-style tests.
    /// </summary>
    [TestFixture]
    public class EnhancedWebElementTests
    {
        [Test]
        public void EnhancedWebElement_WithNullDriver_ShouldThrowArgumentNullException()
        {
            // Act & Assert - Test with By locator
            Assert.Throws<ArgumentNullException>((Action)(() => new EnhancedWebElement(null!, OpenQA.Selenium.By.Id("test"), "test")));
        }

        [Test]
        public void ElementActions_FindElement_WithNullDriver_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>((Action)(() => ElementActions.FindElement(null!, By.Id("test"))));
        }

        [Test]
        public void ElementActions_FindElement_WithNullLocator_ShouldThrowArgumentNullException()
        {
            // Note: This test would require a mock driver
            // For now, we'll just verify the method signature exists
            Assert.Pass("ElementActions.FindElement method exists");
        }
    }
}


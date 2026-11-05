using NUnit.Framework;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.IO;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for the Logger class.
    /// </summary>
    [TestFixture]
    public class LoggerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Initialize logger before each test
            Logger.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            Logger.CloseAndFlush();
        }

        [Test]
        public void Initialize_ShouldCreateLoggerInstance()
        {
            // Act
            Logger.Initialize();

            // Assert
            Assert.That(Logger.Instance, Is.Not.Null);
        }

        [Test]
        public void SetCorrelationId_ShouldSetCorrelationId()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();

            // Act
            Logger.SetCorrelationId(correlationId);

            // Assert
            Assert.That(Logger.CorrelationId, Is.EqualTo(correlationId));
        }

        [Test]
        public void SetTestId_ShouldSetTestId()
        {
            // Arrange
            var testId = "Test-123";

            // Act
            Logger.SetTestId(testId);

            // Assert
            Assert.That(Logger.TestId, Is.EqualTo(testId));
        }

        [Test]
        public void SetBrowser_ShouldSetBrowser()
        {
            // Arrange
            var browser = "Chrome";

            // Act
            Logger.SetBrowser(browser);

            // Assert
            Assert.That(Logger.Browser, Is.EqualTo(browser));
        }

        [Test]
        public void SetEnvironment_ShouldSetEnvironment()
        {
            // Arrange
            var environment = "QA";

            // Act
            Logger.SetEnvironment(environment);

            // Assert
            Assert.That(Logger.Environment, Is.EqualTo(environment));
        }

        [Test]
        public void Information_ShouldLogInformationMessage()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow((Action)(() => Logger.Information("Test information message")));
        }

        [Test]
        public void Warning_ShouldLogWarningMessage()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow((Action)(() => Logger.Warning("Test warning message")));
        }

        [Test]
        public void Error_ShouldLogErrorMessage()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow((Action)(() => Logger.Error("Test error message")));
        }

        [Test]
        public void Error_WithException_ShouldLogException()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert - Should not throw
            Assert.DoesNotThrow((Action)(() => Logger.Error(exception, "Test error with exception")));
        }

        [Test]
        public void Debug_ShouldLogDebugMessage()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow((Action)(() => Logger.Debug("Test debug message")));
        }
    }
}


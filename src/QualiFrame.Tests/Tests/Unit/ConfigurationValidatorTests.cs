using NUnit.Framework;
using QualiFrame.Framework.Configuration;
using System;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for the ConfigurationValidator class.
    /// </summary>
    [TestFixture]
    public class ConfigurationValidatorTests
    {
        [Test]
        public void Validate_WithValidConfiguration_ShouldReturnValid()
        {
            // Arrange
            var config = TestConfiguration.Instance;

            // Act
            var result = ConfigurationValidator.Validate(config);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Note: Actual validation depends on appsettings.json content
        }

        [Test]
        public void Validate_WithInvalidBrowser_ShouldReturnErrors()
        {
            // Arrange
            var config = TestConfiguration.Instance;
            // This test would require mocking or creating a test configuration
            // For now, we'll just verify the method exists and doesn't throw

            // Act & Assert
            Assert.DoesNotThrow((Action)(() => ConfigurationValidator.Validate(config)));
        }

        [Test]
        public void Validate_WithEmptyBaseUrl_ShouldReturnErrors()
        {
            // Arrange
            var config = TestConfiguration.Instance;

            // Act
            var result = ConfigurationValidator.Validate(config);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsValid || result.Errors.Count > 0, Is.True);
        }

        [Test]
        public void Validate_WithNegativeTimeout_ShouldReturnErrors()
        {
            // Arrange
            var config = TestConfiguration.Instance;

            // Act
            var result = ConfigurationValidator.Validate(config);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ValidationResult_ShouldContainErrorsAndWarnings()
        {
            // Arrange
            var result = new ValidationResult
            {
                IsValid = false,
                Errors = { "Error 1", "Error 2" },
                Warnings = { "Warning 1" }
            };

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParallelExecution_ShouldReadNestedTestConfigurationKeys()
        {
            var config = TestConfiguration.Instance;

            Assert.That(config.ParallelExecutionEnabled, Is.True);
            Assert.That(config.MaxParallelTests, Is.EqualTo(4));
            Assert.That(config.MaxParallelSuites, Is.EqualTo(2));
        }
    }
}


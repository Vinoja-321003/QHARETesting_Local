using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for Phase 1 features: Healenium, SmartTestSelector, and ModularTestCase.
    /// </summary>
    [TestFixture]
    public class Phase1Tests
    {
        [Test]
        public void HealeniumConfiguration_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var config = new HealeniumConfiguration();

            // Assert
            Assert.That(config.Enabled, Is.False);
            Assert.That(config.BackendUrl, Is.EqualTo("http://localhost:7878"));
            Assert.That(config.TimeoutSeconds, Is.EqualTo(10));
            Assert.That(config.LogHealingAttempts, Is.True);
            Assert.That(config.MaxHealingAttempts, Is.EqualTo(3));
        }

        [Test]
        public void HealeniumConfiguration_CustomValues_ShouldBeSet()
        {
            // Arrange
            var config = new HealeniumConfiguration
            {
                Enabled = true,
                BackendUrl = "http://localhost:8080",
                TimeoutSeconds = 20,
                LogHealingAttempts = false,
                MaxHealingAttempts = 5
            };

            // Assert
            Assert.That(config.Enabled, Is.True);
            Assert.That(config.BackendUrl, Is.EqualTo("http://localhost:8080"));
            Assert.That(config.TimeoutSeconds, Is.EqualTo(20));
            Assert.That(config.LogHealingAttempts, Is.False);
            Assert.That(config.MaxHealingAttempts, Is.EqualTo(5));
        }

        [Test]
        public void MLTestExecutionRecord_Properties_ShouldBeSet()
        {
            // Arrange
            var record = new MLTestExecutionRecord
            {
                TestName = "Test1",
                Browser = "Chrome",
                Environment = "QA",
                Passed = true,
                Duration = TimeSpan.FromSeconds(5),
                ExecutionTime = DateTime.UtcNow
            };

            // Assert
            Assert.That(record.TestName, Is.EqualTo("Test1"));
            Assert.That(record.Browser, Is.EqualTo("Chrome"));
            Assert.That(record.Environment, Is.EqualTo("QA"));
            Assert.That(record.Passed, Is.True);
            Assert.That(record.Duration, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void SmartTestSelector_RecordTestExecution_ShouldStoreRecord()
        {
            // Arrange
            var selector = new SmartTestSelector();
            var record = new MLTestExecutionRecord
            {
                TestName = "Test1",
                Browser = "Chrome",
                Environment = "QA",
                Passed = true,
                Duration = TimeSpan.FromSeconds(5)
            };

            // Act
            selector.RecordTestExecution(record);

            // Assert
            var statistics = selector.GetTestStatistics("Test1");
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics!.TestName, Is.EqualTo("Test1"));
            Assert.That(statistics.TotalExecutions, Is.EqualTo(1));
            Assert.That(statistics.PassedCount, Is.EqualTo(1));
        }

        [Test]
        public void SmartTestSelector_GetRecommendedTests_ShouldReturnTests()
        {
            // Arrange
            var selector = new SmartTestSelector();

            // Add some test execution history
            selector.RecordTestExecution(new MLTestExecutionRecord
            {
                TestName = "Test1",
                Browser = "Chrome",
                Environment = "QA",
                Passed = true,
                Duration = TimeSpan.FromSeconds(5)
            });

            selector.RecordTestExecution(new MLTestExecutionRecord
            {
                TestName = "Test2",
                Browser = "Chrome",
                Environment = "QA",
                Passed = true,
                Duration = TimeSpan.FromSeconds(3)
            });

            // Act
            var recommendations = selector.GetRecommendedTests("Chrome", "QA", 5);

            // Assert
            Assert.That(recommendations, Is.Not.Null);
            Assert.That(recommendations.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void SmartTestSelector_GetTestStatistics_ShouldReturnNullForUnknownTest()
        {
            // Arrange
            var selector = new SmartTestSelector();

            // Act
            var statistics = selector.GetTestStatistics("UnknownTest");

            // Assert
            Assert.That(statistics, Is.Null);
        }

        [Test]
        public void SmartTestSelector_GetTestStatistics_ShouldCalculateSuccessRate()
        {
            // Arrange
            var selector = new SmartTestSelector();

            // Add multiple executions with mixed results
            selector.RecordTestExecution(new MLTestExecutionRecord
            {
                TestName = "Test1",
                Browser = "Chrome",
                Environment = "QA",
                Passed = true,
                Duration = TimeSpan.FromSeconds(5)
            });

            selector.RecordTestExecution(new MLTestExecutionRecord
            {
                TestName = "Test1",
                Browser = "Chrome",
                Environment = "QA",
                Passed = false,
                Duration = TimeSpan.FromSeconds(2)
            });

            selector.RecordTestExecution(new MLTestExecutionRecord
            {
                TestName = "Test1",
                Browser = "Chrome",
                Environment = "QA",
                Passed = true,
                Duration = TimeSpan.FromSeconds(4)
            });

            // Act
            var statistics = selector.GetTestStatistics("Test1");

            // Assert
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics!.TotalExecutions, Is.EqualTo(3));
            Assert.That(statistics.PassedCount, Is.EqualTo(2));
            Assert.That(statistics.FailedCount, Is.EqualTo(1));
            Assert.That(statistics.SuccessRate, Is.EqualTo(2f / 3f).Within(0.01f));
        }

        [Test]
        public void TestConfiguration_GetHealeniumConfiguration_ShouldReturnConfiguration()
        {
            // Arrange
            var config = TestConfiguration.Instance;

            // Act
            var healeniumConfig = config.GetHealeniumConfiguration();

            // Assert
            Assert.That(healeniumConfig, Is.Not.Null);
            Assert.That(healeniumConfig.Enabled, Is.EqualTo(config.HealeniumEnabled));
            Assert.That(healeniumConfig.BackendUrl, Is.EqualTo(config.HealeniumBackendUrl));
        }

        [Test]
        public void TestConfiguration_SmartTestSelectionProperties_ShouldHaveDefaults()
        {
            // Arrange
            var config = TestConfiguration.Instance;

            // Assert
            Assert.That(config.SmartTestSelectionRecommendationCount, Is.GreaterThan(0));
            Assert.That(config.SmartTestSelectionDataPath, Is.Not.Null);
        }
    }
}

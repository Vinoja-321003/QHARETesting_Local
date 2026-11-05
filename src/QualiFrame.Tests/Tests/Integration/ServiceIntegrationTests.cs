using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using QualiFrame.Framework.Core.Services;
using System;
using System.Threading.Tasks;

namespace QualiFrame.Tests.Tests.Integration
{
    /// <summary>
    /// Integration tests for service layer.
    /// </summary>
    [TestFixture]
    public class ServiceIntegrationTests : IntegrationTestBase
    {
        [Test]
        public void TestExecutionService_CanBeResolved()
        {
            // Act
            var service = ServiceProvider.GetService(typeof(ITestExecutionService)) as ITestExecutionService;

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void ReportingService_CanBeResolved()
        {
            // Act
            var service = ServiceProvider.GetService(typeof(IReportingService)) as IReportingService;

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void TestExecutionService_ExecuteTest_WithValidAction_ReturnsTrue()
        {
            // Arrange
            var service = ServiceProvider.GetService(typeof(ITestExecutionService)) as ITestExecutionService;
            if (service == null)
            {
                Assert.Fail("Service not found");
                return;
            }
            bool actionExecuted = false;

            // Act
            var result = service.ExecuteTest(Driver, "TestScenario", driver =>
            {
                actionExecuted = true;
                return true;
            });

            // Assert
            Assert.That(result, Is.True);
            Assert.That(actionExecuted, Is.True);
        }

        [Test]
        public async Task TestExecutionService_RetryTest_WithFailingThenSucceedingAction_ReturnsTrue()
        {
            // Arrange
            var service = ServiceProvider.GetService(typeof(ITestExecutionService)) as ITestExecutionService;
            if (service == null)
            {
                Assert.Fail("Service not found");
                return;
            }
            int attemptCount = 0;

            // Act
            var result = await service.RetryTestAsync(async () =>
            {
                await Task.CompletedTask; // Ensure async method has await
                attemptCount++;
                if (attemptCount < 2)
                {
                    return false;
                }
                return true;
            }, maxRetries: 3);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(attemptCount, Is.EqualTo(2));
        }
    }
}


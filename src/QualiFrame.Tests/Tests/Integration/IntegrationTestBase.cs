using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.DependencyInjection;
using QualiFrame.Framework.Utilities.WebDriver;
using System;

namespace QualiFrame.Tests.Tests.Integration
{
    /// <summary>
    /// Base class for integration tests providing common setup and teardown.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public abstract class IntegrationTestBase
    {
        protected IServiceProvider ServiceProvider { get; private set; } = null!;
        protected IWebDriver Driver { get; private set; } = null!;
        protected TestConfiguration Config { get; private set; } = null!;

        [SetUp]
        public virtual void SetUp()
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddQualiFrameServices();
            ServiceProvider = services.BuildServiceProvider();

            // Get configuration
            Config = ServiceProvider.GetService(typeof(TestConfiguration)) as TestConfiguration 
                ?? TestConfiguration.Instance;

            // Initialize driver (can be overridden to use driver pool)
            Driver = EnhancedWebDriverFactory.CreateDriver();
        }

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                Driver?.Quit();
                Driver?.Dispose();
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
            finally
            {
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}


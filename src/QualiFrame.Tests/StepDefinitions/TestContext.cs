using Microsoft.Extensions.DependencyInjection;
using QualiFrame.Framework.Core.DependencyInjection;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Interfaces;
using QualiFrame.Framework.Utilities.WebDriver;
using System;

namespace QualiFrame.Tests.StepDefinitions
{
    /// <summary>
    /// Test context providing dependency injection services for tests.
    /// </summary>
    public class TestContext
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the TestContext class.
        /// </summary>
        public TestContext()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Configures services for dependency injection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Add all QualiFrame framework services
            services.AddQualiFrameServices();

            // Register TestContext itself
            services.AddTransient<TestContext>();
        }

        public T? GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }

        public T GetRequiredService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            _serviceProvider?.GetService<IDisposable>()?.Dispose();
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QualiFrame.Framework.API;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Interfaces;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Framework.Data.Repositories;
using QualiFrame.Framework.Orchestration;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Reporting;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Security;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;
using System;
using System.Linq;

namespace QualiFrame.Framework.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring dependency injection services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all QualiFrame framework services to the service collection.
        /// </summary>
        public static IServiceCollection AddQualiFrameServices(this IServiceCollection services)
        {
            services.AddSingleton<TestConfiguration>(provider => TestConfiguration.Instance);
            services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();

            services.AddSingleton<Serilog.ILogger>(provider => Logger.Instance);

            services.AddSingleton<SecretsManager>();

            services.AddSingleton<IWebDriverFactory, WebDriverFactoryWrapper>();

            services.AddSingleton<ExcelDataRepository>();
            services.AddSingleton<CsvDataRepository>();
            services.AddSingleton<JsonDataRepository>();
            services.AddSingleton<SqlDataRepository>();

            services.AddTransient<EnhancedApiClient>();
            services.AddTransient<QualiFrame.Framework.API.GraphQL.GraphQLClient>();

            services.AddSingleton<ExtentReportManager>();
            services.AddSingleton<AllureReporter>();
            services.AddSingleton<ReportGenerator>();

            services.AddSingleton<TestMetrics>();
            services.AddSingleton<TestAnalytics>();

            services.AddSingleton<TestDependencyGraph>();
            services.AddSingleton<TestOrchestrator>();


            services.AddSingleton<ITestExecutionService>(provider =>
            {
                var testMetrics = provider.GetService(typeof(TestMetrics)) as TestMetrics 
                    ?? throw new InvalidOperationException("TestMetrics not registered");
                return new TestExecutionService(testMetrics);
            });
            services.AddSingleton<IReportingService, ReportingService>();

            return services;
        }

        /// <summary>
        /// Adds QualiFrame services with custom configuration.
        /// </summary>
        public static IServiceCollection AddQualiFrameServices(
            this IServiceCollection services,
            Action<QualiFrameServiceOptions> configure)
        {
            var options = new QualiFrameServiceOptions();
            configure(options);

            services.AddQualiFrameServices();

            if (options.UseCustomWebDriverFactory != null)
            {
                services.Remove(services.First(s => s.ServiceType == typeof(IWebDriverFactory)));
                services.AddSingleton(typeof(IWebDriverFactory), options.UseCustomWebDriverFactory);
            }

            return services;
        }
    }

    /// <summary>
    /// Options for configuring QualiFrame services.
    /// </summary>
    public class QualiFrameServiceOptions
    {
        /// <summary>
        /// Custom WebDriver factory type to use instead of default.
        /// </summary>
        public Type? UseCustomWebDriverFactory { get; set; }
    }

    /// <summary>
    /// Configuration provider implementation.
    /// </summary>
    internal class ConfigurationProvider : IConfigurationProvider
    {
        private readonly TestConfiguration _config;

        public ConfigurationProvider()
        {
            _config = TestConfiguration.Instance;
        }

        public string? GetValue(string key)
        {
            return _config.GetEncryptedValue(key);
        }

        public string GetValue(string key, string defaultValue)
        {
            return GetValue(key) ?? defaultValue;
        }

        public bool Validate(out string? errorMessage)
        {
            var result = _config.Validate();
            errorMessage = result.IsValid ? null : string.Join("; ", result.Errors);
            return result.IsValid;
        }
    }
}

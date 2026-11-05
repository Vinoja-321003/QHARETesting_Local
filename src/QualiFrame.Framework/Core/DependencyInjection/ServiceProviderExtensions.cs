using Microsoft.Extensions.DependencyInjection;
using QualiFrame.Framework.API;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Interfaces;
using QualiFrame.Framework.Data.Repositories;
using QualiFrame.Framework.Orchestration;
using QualiFrame.Framework.PageBases;
using QualiFrame.Framework.Reporting;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Security;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Framework.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods for retrieving QualiFrame services from the service provider.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Gets the WebDriver factory from the service provider.
        /// </summary>
        public static IWebDriverFactory GetWebDriverFactory(this IServiceProvider provider)
        {
            return provider.GetRequiredService<IWebDriverFactory>();
        }

        /// <summary>
        /// Gets the test configuration from the service provider.
        /// </summary>
        public static TestConfiguration GetTestConfiguration(this IServiceProvider provider)
        {
            return provider.GetRequiredService<TestConfiguration>();
        }

        /// <summary>
        /// Gets the logger instance (static access).
        /// </summary>
        public static Serilog.ILogger GetLogger(this IServiceProvider provider)
        {
            return Logger.Instance;
        }

        /// <summary>
        /// Gets the secrets manager from the service provider.
        /// </summary>
        public static SecretsManager GetSecretsManager(this IServiceProvider provider)
        {
            return provider.GetRequiredService<SecretsManager>();
        }

        /// <summary>
        /// Gets a data repository from the service provider.
        /// </summary>
        public static IDataRepository GetDataRepository(
            this IServiceProvider provider, 
            string sourceType = "EXCEL")
        {
            return DataRepositoryFactory.CreateRepository(sourceType);
        }

        /// <summary>
        /// Gets the enhanced API client from the service provider.
        /// </summary>
        public static EnhancedApiClient GetApiClient(this IServiceProvider provider)
        {
            return provider.GetRequiredService<EnhancedApiClient>();
        }

        /// <summary>
        /// Gets the Extent report manager from the service provider.
        /// </summary>
        public static ExtentReportManager GetExtentReportManager(this IServiceProvider provider)
        {
            return provider.GetRequiredService<ExtentReportManager>();
        }

        /// <summary>
        /// Gets the Allure reporter from the service provider.
        /// </summary>
        public static AllureReporter GetAllureReporter(this IServiceProvider provider)
        {
            return provider.GetRequiredService<AllureReporter>();
        }

        /// <summary>
        /// Gets the test analytics from the service provider.
        /// </summary>
        public static TestAnalytics GetTestAnalytics(this IServiceProvider provider)
        {
            return provider.GetRequiredService<TestAnalytics>();
        }

        /// <summary>
        /// Gets the test orchestrator from the service provider.
        /// </summary>
        public static TestOrchestrator GetTestOrchestrator(this IServiceProvider provider)
        {
            return provider.GetRequiredService<TestOrchestrator>();
        }

    }
}

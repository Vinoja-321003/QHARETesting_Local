using Microsoft.Extensions.Configuration;

namespace QualiFrame.Runner.Core
{
    /// <summary>
    /// Minimal configuration for the CLI runner. Reads the same appsettings.json as the framework
    /// without referencing QualiFrame.Framework (avoids copying Selenium, ML.NET, etc. into Runner output).
    /// </summary>
    public sealed class RunnerConfiguration
    {
        private static RunnerConfiguration? _instance;
        private static readonly object Lock = new();
        private readonly IConfiguration _configuration;

        private RunnerConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var configBuilder = new ConfigurationBuilder().SetBasePath(baseDirectory);

            var baseConfigPaths = new[]
            {
                Path.Combine(baseDirectory, "Configuration", "appsettings.json"),
                Path.Combine(baseDirectory, "..", "..", "..", "..", "Configuration", "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "QualiFrame.Framework", "Configuration", "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "QualiFrame.Tests", "Configuration", "appsettings.json")
            };

            var baseConfigPath = baseConfigPaths.FirstOrDefault(File.Exists);
            if (baseConfigPath != null)
            {
                var configDir = Path.GetDirectoryName(baseConfigPath);
                configBuilder.SetBasePath(configDir ?? baseDirectory);
                configBuilder.AddJsonFile(Path.GetFileName(baseConfigPath), optional: true, reloadOnChange: true);

                var envConfigFile = Path.Combine(configDir ?? baseDirectory, $"appsettings.{environment}.json");
                if (File.Exists(envConfigFile))
                {
                    configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                }
            }
            else
            {
                configBuilder.AddJsonFile("Configuration/appsettings.json", optional: true, reloadOnChange: true)
                             .AddJsonFile($"Configuration/appsettings.{environment}.json", optional: true, reloadOnChange: true);
            }

            configBuilder.AddEnvironmentVariables();
            _configuration = configBuilder.Build();
        }

        public static RunnerConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        _instance ??= new RunnerConfiguration();
                    }
                }

                return _instance;
            }
        }

        public bool ParallelExecutionEnabled => bool.Parse(FirstConfigured(
            "TestConfiguration:ParallelExecution:Enable",
            "ParallelExecution:Enable") ?? "true");
        public int MaxParallelTests => int.Parse(FirstConfigured(
            "TestConfiguration:ParallelExecution:MaxParallelTests",
            "ParallelExecution:MaxParallelTests") ?? "4");
        public string ExtentReportPath => _configuration["Reporting:ExtentReport:ReportPath"] ?? "Results/Reports/ExtentReport.html";
        public string AllureResultsPath => _configuration["Reporting:AllureReport:ResultsPath"] ?? "Results/AllureResults";
        public string ScreenshotsPath => _configuration["Reporting:Screenshots:Path"] ?? "Results/Screenshots";
        public string LogPath => _configuration["Logging:File:Path"] ?? "Results/Logs/";

        private string? FirstConfigured(params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = _configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace QualiFrame.Framework.Configuration
{
    public class TestConfiguration
    {
        private static TestConfiguration? _instance;
        private static readonly object _lock = new object();
        private readonly IConfiguration _configuration;

        private TestConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            
            // Try to find configuration file in multiple locations
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // IMPORTANT: Environment variables must be added LAST so they can override JSON defaults
            // (e.g., in CI or on non-Windows hosts where desktop automation should be disabled).
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(baseDirectory);

            // Try to add base configuration file from various possible locations
            var baseConfigPaths = new[]
            {
                Path.Combine(baseDirectory, "Configuration", "appsettings.json"),
                Path.Combine(baseDirectory, "..", "..", "..", "..", "Configuration", "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "QualiFrame.Framework", "Configuration", "appsettings.json")
            };

            var baseConfigPath = baseConfigPaths.FirstOrDefault(File.Exists);
            if (baseConfigPath != null)
            {
                var configDir = Path.GetDirectoryName(baseConfigPath);
                configBuilder.SetBasePath(configDir ?? baseDirectory);
                configBuilder.AddJsonFile(Path.GetFileName(baseConfigPath), optional: true, reloadOnChange: true);
                
                // Add environment-specific file if it exists
                var envConfigFile = Path.Combine(configDir ?? baseDirectory, $"appsettings.{environment}.json");
                if (File.Exists(envConfigFile))
                {
                    configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                }

                // Gitignored local overlay for credentials (appsettings.Local.json)
                var localConfigFile = Path.Combine(configDir ?? baseDirectory, "appsettings.Local.json");
                if (File.Exists(localConfigFile))
                {
                    configBuilder.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
                }
            }
            else
            {
                // If no config file found, try relative path (for when running from bin folder)
                configBuilder.AddJsonFile("Configuration/appsettings.json", optional: true, reloadOnChange: true)
                             .AddJsonFile($"Configuration/appsettings.{environment}.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("Configuration/appsettings.Local.json", optional: true, reloadOnChange: true);
            }

            configBuilder.AddEnvironmentVariables();
            _configuration = configBuilder.Build();
        }

        public static TestConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TestConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        public string Browser => _configuration["TestConfiguration:Browser"] ?? "Chrome";
        public bool Headless => bool.Parse(_configuration["TestConfiguration:Headless"] ?? "false");
        public int ImplicitWait => int.Parse(_configuration["TestConfiguration:ImplicitWait"] ?? "10");
        public int ImplicitWaitSeconds => int.Parse(_configuration["TestConfiguration:ImplicitWaitSeconds"] ?? "10");
        public int PageLoadTimeout => int.Parse(_configuration["TestConfiguration:PageLoadTimeout"] ?? "30");
        public int PageLoadTimeoutSeconds => int.Parse(_configuration["TestConfiguration:PageLoadTimeoutSeconds"] ?? "30");
        public int ScriptTimeout => int.Parse(_configuration["TestConfiguration:ScriptTimeout"] ?? "30");
        public int ScriptTimeoutSeconds => int.Parse(_configuration["TestConfiguration:ScriptTimeoutSeconds"] ?? "30");
        public bool WindowMaximize => bool.Parse(_configuration["TestConfiguration:WindowMaximize"] ?? "true");
        public bool ScreenshotOnFailure => bool.Parse(_configuration["TestConfiguration:ScreenshotOnFailure"] ?? "true");
        public bool VideoRecording => bool.Parse(_configuration["TestConfiguration:VideoRecording"] ?? "false");

        public string BaseUrl => _configuration["Environments:QA:BaseUrl"] ?? "https://www.saucedemo.com";
        public string ApiBaseUrl => _configuration["Environments:QA:ApiBaseUrl"] ?? "https://petstore.swagger.io/v2";
        public string DatabaseConnectionString => _configuration["Environments:QA:DatabaseConnectionString"] ?? "";
        public string RemoteWebDriverUrl => _configuration["TestConfiguration:RemoteWebDriverUrl"] ?? "";
        public string ScreenshotPath => _configuration["Reporting:Screenshots:Path"] ?? "Results/Screenshots";

        public string DefaultUsername => _configuration["TestData:DefaultUser:Username"] ?? "standard_user";
        public string DefaultPassword => _configuration["TestData:DefaultUser:Password"] ?? "secret_sauce";
        public string LockedUsername => _configuration["TestData:LockedUser:Username"] ?? "locked_out_user";
        public string LockedPassword => _configuration["TestData:LockedUser:Password"] ?? "secret_sauce";
        public string ProblemUsername => _configuration["TestData:ProblemUser:Username"] ?? "problem_user";
        public string ProblemPassword => _configuration["TestData:ProblemUser:Password"] ?? "secret_sauce";
        public string PerformanceUsername => _configuration["TestData:PerformanceUser:Username"] ?? "performance_glitch_user";
        public string PerformancePassword => _configuration["TestData:PerformanceUser:Password"] ?? "secret_sauce";

        public bool ExtentReportEnabled => bool.Parse(_configuration["Reporting:ExtentReport:Enable"] ?? "true");
        public string ExtentReportPath => _configuration["Reporting:ExtentReport:ReportPath"] ?? "Results/Reports/ExtentReport.html";
        public string ExtentReportTheme => _configuration["Reporting:ExtentReport:Theme"] ?? "Standard";
        public string ExtentReportTitle => _configuration["Reporting:ExtentReport:DocumentTitle"] ?? "QualiFrame Test Report";
        public string ExtentReportName => _configuration["Reporting:ExtentReport:ReportName"] ?? "QualiFrame Automation Test Results";

        public bool AllureReportEnabled => bool.Parse(_configuration["Reporting:AllureReport:Enable"] ?? "true");
        public string AllureResultsPath => _configuration["Reporting:AllureReport:ResultsPath"] ?? "Results/AllureResults";

        public bool ScreenshotsEnabled => bool.Parse(_configuration["Reporting:Screenshots:Enable"] ?? "true");
        public string ScreenshotsPath => _configuration["Reporting:Screenshots:Path"] ?? "Results/Screenshots";
        public string ScreenshotsFormat => _configuration["Reporting:Screenshots:Format"] ?? "PNG";

        public bool ParallelExecutionEnabled => bool.Parse(FirstConfigured(
            "TestConfiguration:ParallelExecution:Enable",
            "ParallelExecution:Enable") ?? "true");
        public int MaxParallelTests => int.Parse(FirstConfigured(
            "TestConfiguration:ParallelExecution:MaxParallelTests",
            "ParallelExecution:MaxParallelTests") ?? "4");
        public int MaxParallelSuites => int.Parse(FirstConfigured(
            "TestConfiguration:ParallelExecution:MaxParallelSuites",
            "ParallelExecution:MaxParallelSuites") ?? "2");

        public bool SauceLabsEnabled => bool.Parse(_configuration["CloudTesting:SauceLabs:Enable"] ?? "false");
        public string SauceLabsUsername => _configuration["CloudTesting:SauceLabs:Username"] ?? "";
        public string SauceLabsAccessKey => _configuration["CloudTesting:SauceLabs:AccessKey"] ?? "";
        public string SauceLabsRegion => _configuration["CloudTesting:SauceLabs:Region"] ?? "us-west-1";

        public bool BrowserStackEnabled => bool.Parse(_configuration["CloudTesting:BrowserStack:Enable"] ?? "false");
        public string BrowserStackUsername => _configuration["CloudTesting:BrowserStack:Username"] ?? "";
        public string BrowserStackAccessKey => _configuration["CloudTesting:BrowserStack:AccessKey"] ?? "";

        public bool LambdaTestEnabled => bool.Parse(_configuration["CloudTesting:LambdaTest:Enable"] ?? "false");
        public string LambdaTestUsername => _configuration["CloudTesting:LambdaTest:Username"] ?? "";
        public string LambdaTestAccessKey => _configuration["CloudTesting:LambdaTest:AccessKey"] ?? "";

        public bool PerfectoEnabled => bool.Parse(_configuration["CloudTesting:Perfecto:Enable"] ?? "false");
        public string PerfectoUsername => _configuration["CloudTesting:Perfecto:Username"] ?? "";
        public string PerfectoAccessKey => _configuration["CloudTesting:Perfecto:AccessKey"] ?? "";
        public string PerfectoCloudName => _configuration["CloudTesting:Perfecto:CloudName"] ?? "";

        public bool ExperitestEnabled => bool.Parse(_configuration["CloudTesting:Experitest:Enable"] ?? "false");
        public string ExperitestUsername => _configuration["CloudTesting:Experitest:Username"] ?? "";
        public string ExperitestAccessKey => _configuration["CloudTesting:Experitest:AccessKey"] ?? "";

        public bool AwsDeviceFarmEnabled => bool.Parse(_configuration["CloudTesting:AwsDeviceFarm:Enable"] ?? "false");
        public string AwsDeviceFarmAccessKeyId => _configuration["CloudTesting:AwsDeviceFarm:AccessKeyId"] ?? "";
        public string AwsDeviceFarmSecretAccessKey => _configuration["CloudTesting:AwsDeviceFarm:SecretAccessKey"] ?? "";
        public string AwsDeviceFarmRegion => _configuration["CloudTesting:AwsDeviceFarm:Region"] ?? "us-west-2";

        public string AppiumServerUrl => _configuration["Mobile:AppiumServerUrl"] ?? "http://localhost:4723";
        public string PlatformName => _configuration["Mobile:PlatformName"] ?? "Android";
        public string DeviceName => _configuration["Mobile:DeviceName"] ?? "Android Emulator";
        public string AutomationName => _configuration["Mobile:AutomationName"] ?? "UiAutomator2";
        public string AppPackage => _configuration["Mobile:AppPackage"] ?? "";
        public bool AutoStartAppium => bool.Parse(_configuration["Mobile:AutoStartAppium"] ?? "true");
        public int AppiumStartTimeout => int.Parse(_configuration["Mobile:AppiumStartTimeout"] ?? "30");
        
        public string AppPath => _configuration["Mobile:AppPackage"] ?? _configuration["MobileTesting:Appium:AppPath"] ?? "";
        public string AppActivity => _configuration["MobileTesting:Appium:AppActivity"] ?? "";
        public string MobileBrowserName => _configuration["Mobile:BrowserName"] ?? "";
        public bool MobileAutoWebView => bool.Parse(_configuration["Mobile:AutoWebView"] ?? "false");
        public bool MobileNoReset => bool.Parse(_configuration["Mobile:NoReset"] ?? "true");
        public string MobileWebViewContext => _configuration["Mobile:WebViewContext"] ?? "";

        public bool OpenTelemetryEnabled => bool.Parse(_configuration["Observability:OpenTelemetry:Enabled"] ?? "false");
        public string KafkaBootstrapServers => _configuration["Messaging:Kafka:BootstrapServers"] ?? "";
        public string KafkaTopic => _configuration["Messaging:Kafka:Topic"] ?? "qualiframe-test";
        public string GrpcAddress => _configuration["APITesting:Grpc:Address"] ?? "";
        public string WebSocketUrl => _configuration["APITesting:WebSocket:Url"] ?? "";
        public string S3Bucket => _configuration["Storage:S3:Bucket"] ?? "";
        public string S3Region => _configuration["Storage:S3:Region"] ?? "us-east-1";
        public string AzureBlobConnectionString => _configuration["Storage:AzureBlob:ConnectionString"] ?? "";
        public string AzureBlobContainer => _configuration["Storage:AzureBlob:Container"] ?? "";

        public int ApiTimeout => int.Parse(_configuration["APITesting:Timeout"] ?? "30000");
        public int ApiTimeoutSeconds => int.Parse(_configuration["APITesting:TimeoutSeconds"] ?? "30");
        public int ApiRetryCount => int.Parse(_configuration["APITesting:RetryCount"] ?? "3");

        public int DatabaseConnectionTimeout => int.Parse(_configuration["Database:ConnectionTimeout"] ?? "30");
        public int DatabaseCommandTimeout => int.Parse(_configuration["Database:CommandTimeout"] ?? "30");
        public int DatabaseRetryCount => int.Parse(_configuration["Database:RetryCount"] ?? "3");
        public string DatabaseDefaultTable => _configuration["Database:DefaultTable"] ?? "TestData";

        public string LogLevel => _configuration["Logging:LogLevel:Default"] ?? "Information";
        public string LogPath => _configuration["Logging:File:Path"] ?? "Results/Logs/";
        public string LogFileName => _configuration["Logging:File:FileName"] ?? "qualiframe-{Date}.log";
        public string LogMaxFileSize => _configuration["Logging:File:MaxFileSize"] ?? "10MB";
        public int LogMaxFiles => int.Parse(_configuration["Logging:File:MaxFiles"] ?? "10");

        public string EncryptionKey => _configuration["Security:EncryptionKey"] ?? "YourEncryptionKeyHere";
        public string JwtSecret => _configuration["Security:JwtSecret"] ?? "YourJwtSecretHere";
        public int TokenExpirationMinutes => int.Parse(_configuration["Security:TokenExpirationMinutes"] ?? "60");
        public string? KeyVaultUrl => _configuration["Security:KeyVault:Url"];
        public string? GraphQLEndpoint => _configuration["APITesting:GraphQLEndpoint"];

        public bool AccessibilityEnabled => bool.Parse(_configuration["Accessibility:Enabled"] ?? "false");
        public string AccessibilityWCAGLevel => _configuration["Accessibility:WCAGLevel"] ?? "AA";
        public bool AccessibilityIncludeBestPractices => bool.Parse(_configuration["Accessibility:IncludeBestPractices"] ?? "true");
        public string AccessibilityReportPath => _configuration["Accessibility:ReportPath"] ?? "Results/Accessibility";

        public bool OWASPZAPEnabled => bool.Parse(_configuration["Security:OWASPZAP:Enabled"] ?? "false");
        public string OWASPZAPApiUrl => _configuration["Security:OWASPZAP:ZapApiUrl"] ?? "http://localhost:8080";
        public string OWASPZAPApiKey => _configuration["Security:OWASPZAP:ApiKey"] ?? "";
        public string OWASPZAPReportPath => _configuration["Security:OWASPZAP:ReportPath"] ?? "Results/Security";

        public string SelfHealingProviderName => FirstConfigured("SelfHealing:Provider") ?? "";

        public bool QhareEnabled => bool.Parse(_configuration["Qhare:Enabled"] ?? "true");
        public string QhareBaseUrl => _configuration["Qhare:BaseUrl"] ?? "http://localhost:8000";
        public string QhareProjectId => _configuration["Qhare:ProjectId"] ?? "retail-web";
        public string QhareMode => _configuration["Qhare:Mode"] ?? "AUTO_HEAL";
        public int QhareTimeoutSeconds => int.Parse(_configuration["Qhare:TimeoutSeconds"] ?? "8");
        public bool QhareFailClosed => bool.Parse(_configuration["Qhare:FailClosed"] ?? "true");
        public string QhareFrameworkVersion => _configuration["Qhare:FrameworkVersion"] ?? "4.43";
        public string? QhareApiKey => _configuration["Qhare:ApiKey"];
        public bool QhareLogHealingAttempts => bool.Parse(_configuration["Qhare:LogHealingAttempts"] ?? "true");

        public QhareConfiguration GetQhareConfiguration()
        {
            return new QhareConfiguration
            {
                Enabled = QhareEnabled,
                BaseUrl = QhareBaseUrl,
                ProjectId = QhareProjectId,
                Mode = QhareMode,
                TimeoutSeconds = QhareTimeoutSeconds,
                FailClosed = QhareFailClosed,
                FrameworkVersion = QhareFrameworkVersion,
                ApiKey = string.IsNullOrWhiteSpace(QhareApiKey) ? null : QhareApiKey,
                LogHealingAttempts = QhareLogHealingAttempts
            };
        }

        public SelfHealingProvider ActiveSelfHealingProvider => SelfHealingProviderResolver.Resolve(this);

        public bool HealeniumEnabled => bool.Parse(_configuration["Healenium:Enabled"] ?? "false");
        public string HealeniumBackendUrl => _configuration["Healenium:BackendUrl"] ?? "http://localhost:7878";
        public string HealeniumImitatorUrl => _configuration["Healenium:ImitatorUrl"] ?? "http://localhost:8000";
        public int HealeniumTimeoutSeconds => int.Parse(_configuration["Healenium:TimeoutSeconds"] ?? "10");
        public bool HealeniumLogHealingAttempts => bool.Parse(_configuration["Healenium:LogHealingAttempts"] ?? "true");
        public int HealeniumMaxHealingAttempts => int.Parse(_configuration["Healenium:MaxHealingAttempts"] ?? "3");
        public double HealeniumScoreCap => double.Parse(_configuration["Healenium:ScoreCap"] ?? "0.5", System.Globalization.CultureInfo.InvariantCulture);
        public string HealeniumEvidencePath => _configuration["Healenium:EvidencePath"] ?? "Results/HealeniumEvidence";

        public HealeniumConfiguration GetHealeniumConfiguration()
        {
            return new HealeniumConfiguration
            {
                Enabled = HealeniumEnabled,
                BackendUrl = HealeniumBackendUrl,
                ImitatorUrl = HealeniumImitatorUrl,
                TimeoutSeconds = HealeniumTimeoutSeconds,
                LogHealingAttempts = HealeniumLogHealingAttempts,
                MaxHealingAttempts = HealeniumMaxHealingAttempts,
                ScoreCap = HealeniumScoreCap,
                EvidencePath = HealeniumEvidencePath
            };
        }

        public bool SmartTestSelectionEnabled => bool.Parse(_configuration["SmartTestSelection:Enabled"] ?? "false");
        public int SmartTestSelectionRecommendationCount => int.Parse(_configuration["SmartTestSelection:RecommendationCount"] ?? "10");
        public bool SmartTestSelectionAutoInitialize => bool.Parse(_configuration["SmartTestSelection:AutoInitialize"] ?? "true");
        public string SmartTestSelectionDataPath => _configuration["SmartTestSelection:DataPath"] ?? "Data/TestExecutionHistory.json";

        public string? GetEncryptedValue(string key)
        {
            return _configuration[key];
        }

        /// <summary>
        /// Returns the first non-empty configuration value, supporting nested JSON keys and legacy root keys.
        /// </summary>
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

        public ValidationResult Validate()
        {
            return ConfigurationValidator.Validate(this);
        }

        #region Desktop (FlaUI) Configuration

        /// <summary>
        /// Desktop application identifier for FlaUI.
        /// For Win32/WPF: full path to the .exe (or an executable name on PATH).
        /// For UWP / Store apps: Application User Model ID (AUMID), e.g. <c>Publisher.App_hash!App</c>.
        /// </summary>
        public string DesktopAppId => _configuration["Desktop:AppId"] ?? "";

        /// <summary>
        /// Optional command-line arguments passed when launching an executable or store app.
        /// </summary>
        public string DesktopArguments => _configuration["Desktop:Arguments"] ?? "";

        /// <summary>
        /// Optional working directory for Win32 launches. Empty uses FlaUI's default (current directory).
        /// </summary>
        public string DesktopWorkingDirectory => _configuration["Desktop:WorkingDirectory"] ?? "";

        /// <summary>
        /// Optional process name used to attach to an already running application (without .exe).
        /// When set and a matching process exists, <c>LaunchMode=Auto</c> prefers attach over launch.
        /// </summary>
        public string DesktopProcessName => _configuration["Desktop:ProcessName"] ?? "";

        /// <summary>
        /// Optional main window title used to resolve the application window after launch (useful for Store apps).
        /// </summary>
        public string DesktopWindowTitle => _configuration["Desktop:WindowTitle"] ?? "";

        /// <summary>
        /// UI Automation backend. <c>UIA3</c> (default) for WPF/UWP/WinUI; <c>UIA2</c> for some WinForms apps.
        /// See https://github.com/FlaUI/FlaUI
        /// </summary>
        public string DesktopAutomationType => _configuration["Desktop:AutomationType"] ?? "UIA3";

        /// <summary>
        /// How to obtain the application: <c>Auto</c> (default), <c>Launch</c>, <c>Attach</c>, or <c>StoreApp</c>.
        /// </summary>
        public string DesktopLaunchMode => _configuration["Desktop:LaunchMode"] ?? "Auto";

        /// <summary>
        /// Timeout (in seconds) to wait for the desktop app to launch and expose a main window.
        /// </summary>
        public int DesktopLaunchTimeoutSeconds => int.Parse(_configuration["Desktop:LaunchTimeoutSeconds"] ?? "30");

        /// <summary>
        /// If true, desktop actions will capture per-action screenshots on failure
        /// and attach them to Extent and Allure in addition to scenario-level diagnostics.
        /// Defaults to false to reduce overhead.
        /// </summary>
        public bool DesktopCaptureActionScreenshots => bool.Parse(_configuration["Desktop:CaptureActionScreenshots"] ?? "false");

        #endregion
    }
}

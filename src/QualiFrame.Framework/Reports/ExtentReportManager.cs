using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;
using System;
using System.IO;
using System.Threading;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Configuration;
using System.Runtime.InteropServices;
using System.Linq;

namespace QualiFrame.Framework.Reports
{
    /// <summary>
    /// Thread-safe ExtentReports manager with parallel execution support.
    /// Each scenario is a top-level Spark test (steps as child nodes); feature title is a category.
    /// Scenario/step nodes use AsyncLocal per worker thread.
    /// </summary>
    public class ExtentReportManager
    {
        // Thread-safe singleton for ExtentReports instance (shared across all threads)
        private static readonly Lazy<ExtentReports> _extentReports = new Lazy<ExtentReports>(
            () => InitializeExtentReports(),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        // Per-worker scenario/step context (same thread for BeforeScenario → BeforeStep → AfterScenario).
        // Scenarios are top-level Extent tests so Spark lists each test case with its steps.
        // (Feature-as-CreateTest created empty orphan shells under parallel GetOrAdd races.)
        private static readonly AsyncLocal<string?> _currentFeatureName = new AsyncLocal<string?>();
        private static readonly AsyncLocal<ExtentTest?> _currentScenario = new AsyncLocal<ExtentTest?>();
        private static readonly AsyncLocal<ExtentTest?> _currentStep = new AsyncLocal<ExtentTest?>();

        // Shared configuration (read-only after initialization, so thread-safe)
        private static ExtentSparkReporter? _htmlReporter;
        private static string? _reportPath;
        private static readonly object _lockObject = new object();
        /// <summary>True when Extent Spark HTML is disabled (RazorEngine incompatible on .NET 9+).</summary>
        private static bool _disabledDueToRuntimeIncompatibility = false;
        private static bool _warningLogged = false;

        /// <summary>
        /// Gets the ExtentReports instance (thread-safe).
        /// Uses Lazy&lt;T&gt; for thread-safe initialization.
        /// </summary>
        public static ExtentReports GetExtentReports()
        {
            if (_disabledDueToRuntimeIncompatibility)
            {
                // Return dummy instance to prevent null reference exceptions
                return new ExtentReports();
            }
            
            return _extentReports.Value;
        }

        /// <summary>
        /// Returns true when the current runtime cannot host ExtentReports Spark HTML (RazorEngine).
        /// Extent remains supported on .NET 8; .NET 9 and .NET 10 are incompatible.
        /// </summary>
        private static bool IsExtentIncompatibleRuntime()
        {
#if NET8_0
            return false;
#elif NET9_0_OR_GREATER
            return true;
#else
            try
            {
                var frameworkDescription = RuntimeInformation.FrameworkDescription;
                return frameworkDescription.Contains(".NET 9", StringComparison.OrdinalIgnoreCase)
                       || frameworkDescription.Contains(".NET 10", StringComparison.OrdinalIgnoreCase)
                       || frameworkDescription.Contains("net9", StringComparison.OrdinalIgnoreCase)
                       || frameworkDescription.Contains("net10", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
#endif
        }

        private static void DisableExtentForIncompatibleRuntime(string? detail = null)
        {
            _disabledDueToRuntimeIncompatibility = true;
            if (_warningLogged)
            {
                return;
            }

            var reason = string.IsNullOrWhiteSpace(detail)
                ? "RazorEngine is incompatible with .NET 9 / .NET 10."
                : detail;

            Logger.Warning(
                "ExtentReports Spark HTML is disabled on this runtime. {Reason} " +
                "Use Allure reports, or run on net8.0 for ExtentReports support.",
                reason);
            Console.WriteLine("[WARNING] ExtentReports disabled: not compatible with .NET 9 / .NET 10 (RazorEngine).");
            Console.WriteLine("[INFO] Use Allure reports, or target net8.0 for ExtentReports.");
            _warningLogged = true;
        }

        /// <summary>
        /// Thread-safe initialization of ExtentReports.
        /// Called once by Lazy&lt;T&gt; initialization.
        /// Supports both JSON configuration and properties file (Java-style).
        /// </summary>
        private static ExtentReports InitializeExtentReports()
        {
            var config = TestConfiguration.Instance;
            
            // Try to load from properties file first (Java-style), then fall back to JSON config
            var propertiesConfig = LoadExtentPropertiesConfig();
            var extentEnabled = propertiesConfig?.GetBooleanProperty("extent.reporter.spark.start", config.ExtentReportEnabled) ?? config.ExtentReportEnabled;
            
            if (!extentEnabled)
            {
                Logger.Information("ExtentReport is disabled in configuration. Skipping initialization.");
                return new ExtentReports(); // Return dummy instance
            }

            // .NET 9+ : RazorEngine / Extent Spark cannot generate HTML reliably.
            if (IsExtentIncompatibleRuntime())
            {
                DisableExtentForIncompatibleRuntime();
                return new ExtentReports();
            }

            // Runtime fallback when compile-time TFM is unknown (e.g. unexpected host)
            try
            {
                var frameworkDescription = RuntimeInformation.FrameworkDescription;
                if (frameworkDescription.Contains(".NET 9", StringComparison.OrdinalIgnoreCase)
                    || frameworkDescription.Contains(".NET 10", StringComparison.OrdinalIgnoreCase)
                    || frameworkDescription.Contains("net9", StringComparison.OrdinalIgnoreCase)
                    || frameworkDescription.Contains("net10", StringComparison.OrdinalIgnoreCase))
                {
                    DisableExtentForIncompatibleRuntime($"Detected runtime: {frameworkDescription}");
                    return new ExtentReports();
                }
            }
            catch
            {
                // If we can't detect the framework version, continue and let the try-catch below handle any errors
            }

            lock (_lockObject)
            {
                try
                {
                    var extent = new ExtentReports();
                
                var reportPath = config.ExtentReportPath;
                
                if (!Path.IsPathRooted(reportPath))
                {
                    string? projectRoot = null;
                    var currentDir = Directory.GetCurrentDirectory();
                    var dir = new DirectoryInfo(currentDir);
                    
                    while (dir != null && dir.Exists)
                    {
                        var slnFiles = dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
                        
                        if (slnFiles.Length > 0)
                        {
                            projectRoot = dir.FullName;
                            break;
                        }
                        
                        dir = dir.Parent;
                    }
                    
                    if (string.IsNullOrEmpty(projectRoot))
                    {
                        projectRoot = currentDir;
                        Logger.Warning("Could not find project root (.csproj or .sln). Using current directory: {Dir}", currentDir);
                    }
                    else
                    {
                        Logger.Information("Found project root: {Root}", projectRoot);
                    }
                    
                    reportPath = Path.Combine(projectRoot, reportPath);
                }
                
                    // Use properties file path if available, otherwise use JSON config
                    var reportPathFromProps = propertiesConfig?.GetProperty("extent.reporter.spark.out");
                    if (!string.IsNullOrEmpty(reportPathFromProps))
                    {
                        // Properties file uses relative path, resolve it
                        var baseDir = FindProjectRoot();
                        reportPath = Path.Combine(baseDir, reportPathFromProps, "ExtentSummaryReport.html");
                    }
                    
                    reportPath = Path.GetFullPath(reportPath);
                    _reportPath = reportPath;
                    
                    var reportDirectory = Path.GetDirectoryName(reportPath);
                    if (!string.IsNullOrEmpty(reportDirectory) && !Directory.Exists(reportDirectory))
                    {
                        Directory.CreateDirectory(reportDirectory);
                        Logger.Information("Created ExtentReport directory: {Directory}", reportDirectory);
                    }

                    // Register CodePages encoding provider
                    try 
                    {
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    }
                    catch (Exception encodingEx)
                    {
                        Logger.Warning("Failed to register CodePages: {Error}", encodingEx.Message);
                    }

                    // Try to create the reporter - this may fail on .NET 9.0 due to RazorEngine issues
                    try
                    {
                        _htmlReporter = new ExtentSparkReporter(reportPath);
                        
                        // Use properties file values if available, otherwise use JSON config
                        var docTitle = propertiesConfig?.GetProperty("extent.reporter.spark.documentTitle") ?? config.ExtentReportTitle;
                        var reportName = propertiesConfig?.GetProperty("extent.reporter.spark.reportName") ?? config.ExtentReportName;
                        
                        _htmlReporter.Config.DocumentTitle = docTitle;
                        _htmlReporter.Config.ReportName = reportName;
                        
                        var theme = propertiesConfig?.GetProperty("extent.reporter.spark.theme") ?? config.ExtentReportTheme;
                        if (theme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                        {
                            _htmlReporter.Config.Theme = Theme.Dark;
                        }
                        else
                        {
                            _htmlReporter.Config.Theme = Theme.Standard;
                        }
                        
                        extent.AttachReporter(_htmlReporter);
                        
                        extent.AddSystemInfo("Browser", config.Browser);
                        extent.AddSystemInfo("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");
                        extent.AddSystemInfo("Framework", "QualiFrame C# NUnit");
                        extent.AddSystemInfo("Version", "3.0.0");
                        
                        Logger.Information("ExtentReport initialized successfully. Report will be saved to: {Path}", reportPath);
                        Console.WriteLine($"[EXTENT] Initialized. Path: {reportPath}");
                        
                        return extent;
                    }
                    catch (Exception reporterEx) when (reporterEx.Message.Contains("RazorEngine") || 
                                                         reporterEx.Message.Contains("ITemplate") ||
                                                         reporterEx.Message.Contains("CompiledRazorTemplates"))
                    {
                        DisableExtentForIncompatibleRuntime(reporterEx.Message);
                        return new ExtentReports(); // Return dummy instance
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to initialize ExtentReport: {Error}", ex.Message);
                    Console.WriteLine($"[EXTENT] Failed to initialize: {ex.Message}");
                    return new ExtentReports(); // Return dummy instance
                }
            }
        }

        /// <summary>
        /// Records the active feature name for scenario categorization.
        /// Does not create a top-level Extent test (scenarios are top-level so each test shows its steps).
        /// </summary>
        public static ExtentTest? CreateFeature(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                featureName = "Uncategorized";
            }

            _currentFeatureName.Value = featureName;
            Logger.Debug("ExtentReport feature context set: {FeatureName}", featureName);
            return null;
        }

        /// <summary>
        /// Creates a top-level scenario test using the feature name bound to the current worker (if any).
        /// Prefer <see cref="CreateScenario(string, string)"/> under parallel execution.
        /// </summary>
        public static ExtentTest CreateScenario(string scenarioName)
        {
            return CreateScenario(_currentFeatureName.Value, scenarioName);
        }

        /// <summary>
        /// Creates a top-level scenario test (one Spark left-nav entry per test case) and tags it with the feature name.
        /// </summary>
        public static ExtentTest CreateScenario(string? featureName, string scenarioName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(featureName))
                {
                    featureName = "Uncategorized";
                    Logger.Warning(
                        "ExtentReport CreateScenario called without feature name for '{ScenarioName}'. Using '{FeatureName}'.",
                        scenarioName,
                        featureName);
                }

                _currentFeatureName.Value = featureName;

                ExtentTest scenario;
                lock (_lockObject)
                {
                    scenario = GetExtentReports().CreateTest(scenarioName);
                }

                try
                {
                    scenario.AssignCategory(featureName);
                }
                catch (Exception categoryEx)
                {
                    Logger.Debug("Failed to assign feature category on Extent scenario: {Error}", categoryEx.Message);
                }

                _currentScenario.Value = scenario;
                _currentStep.Value = null;
                Logger.Debug("ExtentReport scenario created: {FeatureName} / {ScenarioName}", featureName, scenarioName);
                return scenario;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create ExtentReport scenario: {ScenarioName}", scenarioName);
                throw;
            }
        }

        /// <summary>
        /// Creates a step under the current scenario (thread-safe using AsyncLocal).
        /// </summary>
        public static ExtentTest? CreateStep(string stepName)
        {
            try
            {
                var scenario = _currentScenario.Value;
                if (scenario == null)
                {
                    Logger.Warning(
                        "Cannot create ExtentReport step '{StepName}'. Scenario not initialized on this worker.",
                        stepName);
                    _currentStep.Value = null;
                    return null;
                }

                ExtentTest step;
                lock (_lockObject)
                {
                    step = scenario.CreateNode(stepName);
                }

                _currentStep.Value = step;
                Logger.Debug("ExtentReport step created: {StepName}", stepName);
                return step;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create ExtentReport step: {StepName}", stepName);
                throw;
            }
        }

        /// <summary>
        /// Gets the current step for the current thread (thread-safe).
        /// Used by ReqnrollExtentAdapter to access the current step.
        /// </summary>
        public static ExtentTest? GetCurrentStep()
        {
            return _currentStep.Value;
        }

        /// <summary>
        /// Gets the current scenario for the current thread (thread-safe).
        /// </summary>
        public static ExtentTest? GetCurrentScenario()
        {
            return _currentScenario.Value;
        }

        /// <summary>
        /// Gets the current feature name for the current thread (no top-level feature node).
        /// </summary>
        public static ExtentTest? GetCurrentFeature()
        {
            // Features are categories on scenario tests; there is no feature ExtentTest node.
            return null;
        }

        /// <summary>
        /// Gets the current feature title bound to this worker, if any.
        /// </summary>
        public static string? GetCurrentFeatureName()
        {
            return _currentFeatureName.Value;
        }

        /// <summary>
        /// Logs a message to the current step (thread-safe).
        /// </summary>
        public static void Log(Status status, string message)
        {
            try
            {
                _currentStep.Value?.Log(status, message);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to log to ExtentReport: {Error}", ex.Message);
            }
        }

        public static void LogPass(string message)
        {
            _currentStep.Value?.Pass(message);
        }

        public static void LogFail(string message)
        {
            _currentStep.Value?.Fail(message);
        }

        public static void LogInfo(string message)
        {
            _currentStep.Value?.Info(message);
        }

        public static void LogWarning(string message)
        {
            _currentStep.Value?.Warning(message);
        }

        public static void LogError(string message)
        {
            _currentStep.Value?.Fail(message);
        }
        
        public static void MarkScenarioAsPassed(string? message = null)
        {
            if (_currentScenario.Value != null)
            {
                _currentScenario.Value.Pass(message ?? "Scenario passed");
                Logger.Debug("ExtentReport scenario marked as passed");
            }
        }
        
        public static void MarkScenarioAsFailed(string? message = null)
        {
            if (_currentScenario.Value != null)
            {
                _currentScenario.Value.Fail(message ?? "Scenario failed");
                Logger.Debug("ExtentReport scenario marked as failed");
            }
        }
        
        public static void MarkScenarioAsSkipped(string? message = null)
        {
            if (_currentScenario.Value != null)
            {
                _currentScenario.Value.Skip(message ?? "Scenario skipped");
                Logger.Debug("ExtentReport scenario marked as skipped");
            }
        }

        /// <summary>
        /// Adds a screenshot to the current step (thread-safe).
        /// Automatically converts absolute paths to relative paths.
        /// </summary>
        public static void AddScreenshot(string screenshotPath, string title = "Screenshot")
        {
            try
            {
                if (_currentStep.Value == null)
                {
                    Logger.Warning("Cannot add screenshot to Extent report. Step not initialized.");
                    return;
                }
                
                if (!File.Exists(screenshotPath))
                {
                    Logger.Warning("Cannot add screenshot to Extent report. File not found: {Path}", screenshotPath);
                    return;
                }
                
                var relativePath = ConvertToRelativePath(screenshotPath, _reportPath);
                _currentStep.Value.AddScreenCaptureFromPath(relativePath, title);
                Logger.Debug("Screenshot added to Extent report: {Path} (relative: {Relative}), Title: {Title}", screenshotPath, relativePath, title);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to add screenshot to Extent report: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Loads ExtentReports configuration from properties file (Java-style).
        /// Looks for extent.properties in common locations.
        /// </summary>
        private static PropertiesFileReader? LoadExtentPropertiesConfig()
        {
            try
            {
                var baseDir = FindProjectRoot();
                var possiblePaths = new[]
                {
                    Path.Combine(baseDir, "extent.properties"),
                    Path.Combine(baseDir, "src", "test", "resources", "extent.properties"),
                    Path.Combine(baseDir, "Configuration", "extent.properties"),
                    Path.Combine(Directory.GetCurrentDirectory(), "extent.properties")
                };

                var propertiesPath = possiblePaths.FirstOrDefault(File.Exists);
                if (propertiesPath != null)
                {
                    Logger.Information("Loading ExtentReports configuration from properties file: {Path}", propertiesPath);
                    return PropertiesFileReader.LoadFromFile(propertiesPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to load extent.properties file: {Error}. Using JSON configuration.", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Finds the project root directory by looking for .sln file.
        /// </summary>
        private static string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);
            
            while (dir != null && dir.Exists)
            {
                if (dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            
            return currentDir;
        }

        /// <summary>
        /// Converts an absolute screenshot path to a relative path based on report location.
        /// </summary>
        private static string ConvertToRelativePath(string absolutePath, string? reportPath)
        {
            if (string.IsNullOrEmpty(reportPath))
            {
                return absolutePath;
            }

            try
            {
                var reportDir = Path.GetDirectoryName(reportPath);
                if (string.IsNullOrEmpty(reportDir))
                {
                    return absolutePath;
                }

                var reportDirUri = new Uri(reportDir + Path.DirectorySeparatorChar);
                var screenshotUri = new Uri(absolutePath);
                var relativeUri = reportDirUri.MakeRelativeUri(screenshotUri);
                return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
            }
            catch
            {
                return absolutePath;
            }
        }

        public static void Flush()
        {
            // Skip if disabled due to .NET 9+ / RazorEngine incompatibility
            if (_disabledDueToRuntimeIncompatibility)
            {
                return; // Silently skip - warning already logged during initialization
            }
            
            var config = TestConfiguration.Instance;
            
            if (!config.ExtentReportEnabled)
            {
                Logger.Information("ExtentReport is disabled. Skipping flush.");
                return;
            }
            
            try
            {
                Logger.Information("Flushing ExtentReport...");
                Logger.Information("Report path: {Path}", _reportPath ?? "Not set");
                Logger.Information("Current working directory: {Cwd}", Directory.GetCurrentDirectory());
                
                if (!string.IsNullOrEmpty(_reportPath))
                {
                    var reportDirectory = Path.GetDirectoryName(_reportPath);
                    if (!string.IsNullOrEmpty(reportDirectory) && !Directory.Exists(reportDirectory))
                    {
                        Directory.CreateDirectory(reportDirectory);
                        Logger.Information("Created ExtentReport directory: {Directory}", reportDirectory);
                    }
                }
                
                // Thread-safe flush with error handling for RazorEngine compatibility issues
                lock (_lockObject)
                {
                    try
                    {
                        _extentReports.Value.Flush();
                        Logger.Information("ExtentReport.Flush() called successfully");
                    }
                    catch (ArgumentException argEx) when (argEx.Message.Contains("CompiledRazorTemplates") || 
                                                           argEx.Message.Contains("ITemplate") ||
                                                           argEx.Message.Contains("Expression of type"))
                    {
                        DisableExtentForIncompatibleRuntime(argEx.Message);
                        return; // Exit early since flush failed
                    }
                }
                
                if (!string.IsNullOrEmpty(_reportPath))
                {
                    System.Threading.Thread.Sleep(500);
                    
                    if (File.Exists(_reportPath))
                    {
                        var fileInfo = new FileInfo(_reportPath);
                        Logger.Information("✅ ExtentReport flushed successfully. Report saved to: {Path} (Size: {Size} bytes)", 
                            _reportPath, fileInfo.Length);
                    }
                    else
                    {
                        Logger.Warning("⚠️ ExtentReport flushed but file not found at expected path: {Path}", _reportPath);
                        Logger.Warning("Current working directory: {Cwd}", Directory.GetCurrentDirectory());
                        Logger.Warning("Base directory: {BaseDir}", AppDomain.CurrentDomain.BaseDirectory);
                        
                        var searchPaths = new[]
                        {
                            Path.Combine(Directory.GetCurrentDirectory(), "Results", "Reports", "ExtentReport.html"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results", "Reports", "ExtentReport.html"),
                            Path.Combine(Environment.CurrentDirectory, "Results", "Reports", "ExtentReport.html"),
                            Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "", "Results", "Reports", "ExtentReport.html")
                        };
                        
                        bool found = false;
                        foreach (var searchPath in searchPaths)
                        {
                            try
                            {
                                var fullPath = Path.GetFullPath(searchPath);
                                if (File.Exists(fullPath))
                                {
                                    Logger.Information("✅ Found ExtentReport at alternative location: {Path}", fullPath);
                                    found = true;
                                    break;
                                }
                            }
                            catch
                            {
                                // Skip invalid paths
                            }
                        }
                        
                        if (!found)
                        {
                            Logger.Error("❌ ExtentReport file not found in any expected location. Please check ExtentReports configuration.");
                            Logger.Error("Expected path: {Path}", _reportPath);
                        }
                    }
                }
                else
                {
                    Logger.Warning("Report path is not set. Report may have been flushed but path is unknown.");
                }
            }
            catch (ArgumentException argEx) when (argEx.Message.Contains("CompiledRazorTemplates") || 
                                                   argEx.Message.Contains("ITemplate") ||
                                                   argEx.Message.Contains("Expression of type"))
            {
                DisableExtentForIncompatibleRuntime(argEx.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to flush ExtentReport: {Error}", ex.Message);
                if (ex.StackTrace != null)
                {
                    Logger.Error(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                }
            }
        }
        
        public static string? GetReportPath()
        {
            return _reportPath;
        }

        public static void Close()
        {
            Flush();
        }
        
        /// <summary>
        /// Ensures the ExtentReport is flushed (thread-safe).
        /// </summary>
        public static void EnsureFlushed()
        {
            try
            {
                Flush();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EnsureFlushed: {Error}", ex.Message);
                Console.WriteLine($"[ERROR] Error in EnsureFlushed: {ex.Message}");
            }
        }
    }
}

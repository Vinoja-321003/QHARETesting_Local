using Reqnroll;
using QualiFrame.Framework.Utilities.WebDriver;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.Mobile;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Allure.Net.Commons;
using QualiFrame.Framework.Desktop.Drivers;

namespace QualiFrame.Tests.Hooks
{
    /// <summary>
    /// Reqnroll hooks for test lifecycle management.
    /// Follows Reqnroll hook standards: https://docs.reqnroll.net/latest/automation/hooks.html
    /// Handles browser/context/page lifecycle, failure artifacts, and driver management.
    /// </summary>
    [Binding]
    public class Hooks
    {
        private static readonly TestConfiguration _config = TestConfiguration.Instance;
        private static readonly ExtentReportManager _extentReportManager = new();

        /// <summary>
        /// Finds the project root directory by searching for .csproj or .sln files.
        /// </summary>
        /// <returns>The project root directory path, or current directory if not found.</returns>
        private static string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);
            
            while (dir != null && dir.Exists)
            {
                var slnFiles = dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
                
                if (slnFiles.Length > 0)
                {
                    return dir.FullName;
                }
                
                // If it's a standalone project without sln, we might fallback to csproj,
                // but for this consolidation task, we prefer the solution root.
                // We'll keep looking up if only csproj is found, unless we hit root.
                
                dir = dir.Parent;
            }
            
            return currentDir;
        }

        /// <summary>
        /// Resolves an Allure results directory path (relative or absolute) to an absolute path.
        /// If the path is relative, it's resolved relative to the project root.
        /// </summary>
        /// <param name="path">The path to resolve (can be relative or absolute).</param>
        /// <returns>The resolved absolute path.</returns>
        private static string ResolveAllureResultsDirectory(string path)
        {
            // Always force results to be in the project root "Results" directory
            // This prevents scattered results in bin/ or src/ folders
            var projectRoot = FindProjectRoot();
            
            // If the path contains "Results", just use it relative to root
            // Otherwise force it to Results/AllureResults
            if (path.Contains("AllureResults"))
            {
                 // Extract just the last part or ensure it ends with AllureResults
                 return Path.Combine(projectRoot, "Results", "AllureResults");
            }
            
            return Path.Combine(projectRoot, "Results", "AllureResults");
        }

        /// <summary>
        /// Gets the Allure results directory, checking environment variable first, then configuration.
        /// </summary>
        /// <returns>The resolved absolute path to the Allure results directory.</returns>
        private static string GetAllureResultsDirectory()
        {
            // Check environment variable first (takes precedence)
            var envPath = Environment.GetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY");
            if (!string.IsNullOrEmpty(envPath))
            {
                return Path.GetFullPath(envPath);
            }
            
            // Fall back to configured path
            return ResolveAllureResultsDirectory(_config.AllureResultsPath);
        }

        /// <summary>
        /// Hook executed once before the entire test run.
        /// Initializes framework components, validates configuration, and sets up reporting.
        /// </summary>
        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            // Register CodePages encoding provider to ensure RazorEngine (used by ExtentReports) works correctly on .NET Core/8+
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // Log configuration status
            Console.WriteLine($"[BEFORE_TEST_RUN] Allure reporting enabled: {_config.AllureReportEnabled}");
            Console.WriteLine($"[BEFORE_TEST_RUN] Allure results path: {_config.AllureResultsPath}");
            Logger.Information("BeforeTestRun: Allure reporting enabled: {Enabled}, Path: {Path}", _config.AllureReportEnabled, _config.AllureResultsPath);
            
            // Configure Allure results directory if Allure reporting is enabled
            if (_config.AllureReportEnabled)
            {
                try
                {
                    // Resolve absolute path using helper method
                    var resolvedPath = ResolveAllureResultsDirectory(_config.AllureResultsPath);
                    
                    // Ensure directory exists
                    if (!Directory.Exists(resolvedPath))
                    {
                        Directory.CreateDirectory(resolvedPath);
                        Logger.Information("Created Allure results directory: {Path}", resolvedPath);
                    }
                    
                    // Set environment variable for Allure.NUnit (this takes precedence over config file)
                    Environment.SetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY", resolvedPath);
                    Logger.Information("Set ALLURE_RESULTS_DIRECTORY to: {Path}", resolvedPath);
                    Console.WriteLine($"[INFO] ALLURE_RESULTS_DIRECTORY set to: {resolvedPath}");
                    
                    // Also set it in the current process environment
                    Environment.SetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY", resolvedPath, EnvironmentVariableTarget.Process);
                    
                    // Verify the directory exists and is writable
                    try
                    {
                        var testFile = Path.Combine(resolvedPath, ".test-write");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        Logger.Information("Verified Allure results directory is writable: {Path}", resolvedPath);
                    }
                    catch (Exception writeEx)
                    {
                        Logger.Warning("Allure results directory may not be writable: {Error}", writeEx.Message);
                        Console.WriteLine($"[WARNING] Allure results directory may not be writable: {writeEx.Message}");
                    }
                    
                    // Also update the allureConfig.json file dynamically if it exists in output directory
                    // This ensures the config file has the correct absolute path
                    var outputConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "allureConfig.json");
                    if (File.Exists(outputConfigPath))
                    {
                        try
                        {
                            var configContent = $@"{{
  ""allure"": {{
    ""directory"": ""{resolvedPath.Replace("\\", "\\\\")}"",
    ""links"": [
      ""https://github.com/allure-framework/allure2/issues/{{issue}}"",
      ""https://example.com/tms/{{tms}}""
    ]
  }}
}}";
                            File.WriteAllText(outputConfigPath, configContent);
                            Logger.Debug("Updated allureConfig.json with absolute path: {Path}", resolvedPath);
                        }
                        catch (Exception configEx)
                        {
                            Logger.Warning("Failed to update allureConfig.json: {Error}", configEx.Message);
                        }
                    }
                    
                    // Also set ALLURE_CONFIG to point to the config file (if it exists)
                    // Find project root again for config file location
                    var configCurrentDir = Directory.GetCurrentDirectory();
                    var configDir = new DirectoryInfo(configCurrentDir);
                    string? configProjectRoot = null;
                    
                    while (configDir != null && configDir.Exists)
                    {
                        var csprojFiles = configDir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
                        var slnFiles = configDir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
                        
                        if (csprojFiles.Length > 0 || slnFiles.Length > 0)
                        {
                            configProjectRoot = configDir.FullName;
                            break;
                        }
                        
                        configDir = configDir.Parent;
                    }
                    
                    if (string.IsNullOrEmpty(configProjectRoot))
                    {
                        configProjectRoot = configCurrentDir;
                    }
                    
                    var configFile = Path.Combine(configProjectRoot, "src", "QualiFrame.Tests", "allureConfig.json");
                    if (File.Exists(configFile))
                    {
                        Environment.SetEnvironmentVariable("ALLURE_CONFIG", configFile);
                        Logger.Information("Set ALLURE_CONFIG to: {Path}", configFile);
                    }
                    else
                    {
                        // Try alternative locations
                        var altConfigPaths = new[]
                        {
                            Path.Combine(configCurrentDir, "allureConfig.json"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "allureConfig.json"),
                            Path.Combine(configProjectRoot, "allureConfig.json")
                        };
                        
                        foreach (var altPath in altConfigPaths)
                        {
                            if (File.Exists(altPath))
                            {
                                Environment.SetEnvironmentVariable("ALLURE_CONFIG", altPath);
                                Logger.Information("Set ALLURE_CONFIG to: {Path}", altPath);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to configure Allure results directory: {Error}", ex.Message);
                }
            }
            
            // Initialize logging framework
            Logger.Initialize();
            Logger.SetCorrelationId();
            Logger.Information("Starting test run...");
            
            // Validate configuration
            var validationResult = _config.Validate();
            if (!validationResult.IsValid)
            {
                Logger.Fatal("Configuration validation failed. Please fix the following errors:");
                foreach (var error in validationResult.Errors)
                {
                    Logger.Fatal("  - {Error}", error);
                }
                throw new InvalidOperationException($"Configuration validation failed: {string.Join("; ", validationResult.Errors)}");
            }
            
            // Configure NUnit workers based on config for safer parallel runs
            try 
            { 
                Environment.SetEnvironmentVariable("NUnit.NumberOfTestWorkers", _config.MaxParallelTests.ToString()); 
            } 
            catch (Exception ex)
            {
                Logger.Warning("Failed to set NUnit worker count: {Error}", ex.Message);
            }
            
            // Initialize ExtentReport with explicit logging
            try
            {
                Logger.Information("═══════════════════════════════════════════════════════════");
                Logger.Information("Initializing ExtentReport...");
                Logger.Information("ExtentReport Enabled: {Enabled}", _config.ExtentReportEnabled);
                Logger.Information("ExtentReport Path: {Path}", _config.ExtentReportPath);
                
                var extentReports = ExtentReportManager.GetExtentReports();
                if (extentReports != null)
                {
                    Logger.Information("✅ ExtentReport initialized successfully");
                }
                else
                {
                    Logger.Warning("⚠️ ExtentReport initialization returned null");
                }
                Logger.Information("═══════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ Failed to initialize ExtentReport: {Error}", ex.Message);
                Console.WriteLine($"[ERROR] Failed to initialize ExtentReport: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
            
            // Desktop sessions are started only when a @Desktop scenario or DesktopSessionFactory session runs.
        }

        /// <summary>
        /// Hook executed once after the entire test run.
        /// Performs cleanup, generates reports, and disposes resources.
        /// </summary>
        [AfterTestRun]
        public static void AfterTestRun()
        {
            Logger.Information("═══════════════════════════════════════════════════════════");
            Logger.Information("Test run completed. Starting cleanup and report generation...");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("Test run completed. Starting cleanup and report generation...");
            
            // Cleanup driver pool if used
            try
            {
                DriverPoolManager.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning("Error disposing driver pool: {Error}", ex.Message);
                Console.WriteLine($"[WARNING] Error disposing driver pool: {ex.Message}");
            }
            
            // Generate and save ExtentReport
            try
            {
                Logger.Information("Flushing ExtentReport...");
                Console.WriteLine("[INFO] Flushing ExtentReport...");
                
                if (!_config.ExtentReportEnabled)
                {
                    Logger.Warning("ExtentReport is disabled in configuration. Skipping report generation.");
                    Console.WriteLine("[WARNING] ExtentReport is disabled in configuration. Skipping report generation.");
                }
                else
                {
                    // Force flush multiple times to ensure report is generated
                    try
                    {
                        ExtentReportManager.EnsureFlushed();
                    }
                    catch (Exception flushEx)
                    {
                        Logger.Error(flushEx, "First flush attempt failed: {Error}", flushEx.Message);
                        Console.WriteLine($"[ERROR] First flush attempt failed: {flushEx.Message}");
                        
                        // Try again after a short delay
                        System.Threading.Thread.Sleep(1000);
                        try
                        {
                            ExtentReportManager.EnsureFlushed();
                            Logger.Information("Second flush attempt succeeded");
                            Console.WriteLine("[INFO] Second flush attempt succeeded");
                        }
                        catch (Exception flushEx2)
                        {
                            Logger.Error(flushEx2, "Second flush attempt also failed: {Error}", flushEx2.Message);
                            Console.WriteLine($"[ERROR] Second flush attempt also failed: {flushEx2.Message}");
                        }
                    }
                    
                    // Also try Close() which calls Flush()
                    try
                    {
                        ExtentReportManager.Close();
                    }
                    catch (Exception closeEx)
                    {
                        Logger.Warning("Close() failed (non-critical): {Error}", closeEx.Message);
                    }
                    
                    // Wait for file system to catch up
                    System.Threading.Thread.Sleep(1000);
                    
                    var reportPath = ExtentReportManager.GetReportPath() ?? Path.GetFullPath(_config.ExtentReportPath);
                    Logger.Information("═══════════════════════════════════════════════════════════");
                    Logger.Information("ExtentReport generation completed!");
                    Logger.Information("Report location: {ReportPath}", reportPath);
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                    Console.WriteLine($"ExtentReport generation completed!");
                    Console.WriteLine($"Report location: {reportPath}");
                    
                    // Check multiple possible locations
                    var searchPaths = new[]
                    {
                        reportPath,
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
                                var fileInfo = new FileInfo(fullPath);
                                Logger.Information("✅ Report file found! Size: {Size} bytes", fileInfo.Length);
                                Logger.Information("✅ Report location: {Path}", fullPath);
                                Console.WriteLine($"✅ Report file found! Size: {fileInfo.Length} bytes");
                                Console.WriteLine($"✅ Report location: {fullPath}");
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
                        Logger.Error("❌ Report file NOT found in any expected location");
                        Console.WriteLine("❌ Report file NOT found in any expected location");
                        Console.WriteLine($"Current working directory: {Directory.GetCurrentDirectory()}");
                        Console.WriteLine($"Base directory: {AppDomain.CurrentDomain.BaseDirectory}");
                        Console.WriteLine($"Expected path: {reportPath}");
                        
                        // List all HTML files in Results directory
                        try
                        {
                            var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "Results");
                            if (Directory.Exists(resultsDir))
                            {
                                var htmlFiles = Directory.GetFiles(resultsDir, "*.html", SearchOption.AllDirectories);
                                if (htmlFiles.Length > 0)
                                {
                                    Console.WriteLine($"Found {htmlFiles.Length} HTML file(s) in Results directory:");
                                    foreach (var htmlFile in htmlFiles)
                                    {
                                        Console.WriteLine($"  - {htmlFile}");
                                    }
                                }
                            }
                        }
                        catch (Exception listEx)
                        {
                            Logger.Warning("Failed to list HTML files: {Error}", listEx.Message);
                        }
                    }
                    
                    Logger.Information("═══════════════════════════════════════════════════════════");
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ Failed to generate ExtentReport: {Error}", ex.Message ?? "Unknown error");
                Logger.Error(ex, "Stack trace: {StackTrace}", ex.StackTrace ?? "No stack trace");
                Console.WriteLine($"[ERROR] Failed to generate ExtentReport: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
            
            // Generate Allure Report
            try
            {
                if (_config.AllureReportEnabled)
                {
                    Logger.Information("Generating Allure Report...");
                    Console.WriteLine("[INFO] Generating Allure Report...");
                    
                    GenerateAllureReport();
                }
                else
                {
                    Logger.Information("Allure Report is disabled in configuration. Skipping report generation.");
                    Console.WriteLine("[INFO] Allure Report is disabled in configuration. Skipping report generation.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ Failed to generate Allure Report: {Error}", ex.Message);
                Console.WriteLine($"[ERROR] Failed to generate Allure Report: {ex.Message}");
            }
            
            // Stop Appium server if we started it
            try
            {
                AppiumServerManager.StopAppiumServer();
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to stop Appium server: {Error}", ex.Message);
                Console.WriteLine($"[WARNING] Failed to stop Appium server: {ex.Message}");
            }
            
            // Stop any leftover FlaUI desktop session from this test run.
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    DesktopSessionFactory.Quit();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to stop FlaUI desktop session: {Error}", ex.Message);
                Console.WriteLine($"[WARNING] Failed to stop FlaUI desktop session: {ex.Message}");
            }
            
            Logger.CloseAndFlush();
        }
        
        /// <summary>
        /// Generates Allure report from test results.
        /// </summary>
        private static void GenerateAllureReport()
        {
            try
            {
                // First, try to get the path from environment variable (set by Allure.NUnit or our BeforeTestRun)
                var envResultsPath = Environment.GetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY");
                
                // Also resolve from configuration
                var configResultsPath = _config.AllureResultsPath;
                
                // Build list of possible paths to check
                var possiblePaths = new List<string>();
                
                // Add environment variable path if it exists
                if (!string.IsNullOrEmpty(envResultsPath) && Directory.Exists(envResultsPath))
                {
                    possiblePaths.Add(Path.GetFullPath(envResultsPath));
                }
                
                // Resolve configured path using helper method
                var resolvedConfigPath = ResolveAllureResultsDirectory(configResultsPath);
                possiblePaths.Add(resolvedConfigPath);
                
                // Add common Allure.NUnit default locations
                var currentDir = Directory.GetCurrentDirectory();
                possiblePaths.Add(Path.GetFullPath(Path.Combine(currentDir, "allure-results")));
                possiblePaths.Add(Path.GetFullPath(Path.Combine(currentDir, "Results", "AllureResults")));
                possiblePaths.Add(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "allure-results")));
                possiblePaths.Add(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results", "AllureResults")));
                
                // Try to find project root and add paths relative to it
                var foundProjectRoot = FindProjectRoot();
                
                if (!string.IsNullOrEmpty(foundProjectRoot))
                {
                    possiblePaths.Add(Path.GetFullPath(Path.Combine(foundProjectRoot, "allure-results")));
                    possiblePaths.Add(Path.GetFullPath(Path.Combine(foundProjectRoot, "Results", "AllureResults")));
                    possiblePaths.Add(Path.GetFullPath(Path.Combine(foundProjectRoot, "src", "QualiFrame.Tests", "bin", "Debug", "net8.0", "allure-results")));
                    possiblePaths.Add(Path.GetFullPath(Path.Combine(foundProjectRoot, "src", "QualiFrame.Tests", "bin", "Debug", "net8.0", "Results", "AllureResults")));
                    possiblePaths.Add(Path.GetFullPath(Path.Combine(foundProjectRoot, "src", "QualiFrame.Tests", "bin", "Release", "net8.0", "allure-results")));
                    possiblePaths.Add(Path.GetFullPath(Path.Combine(foundProjectRoot, "src", "QualiFrame.Tests", "bin", "Release", "net8.0", "Results", "AllureResults")));
                }
                
                // Find the first path that exists and has result files
                string? resolvedResultsPath = null;
                foreach (var path in possiblePaths.Distinct())
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            var resultFiles = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                            if (resultFiles.Length > 0)
                            {
                                resolvedResultsPath = path;
                                Logger.Information("Found Allure results in: {Path} ({Count} files)", path, resultFiles.Length);
                                Console.WriteLine($"[INFO] Found Allure results in: {path} ({resultFiles.Length} files)");
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid paths
                    }
                }
                
                // If no results found, use the configured path and create directory
                if (resolvedResultsPath == null)
                {
                    resolvedResultsPath = resolvedConfigPath;
                    
                    // Ensure results directory exists
                    if (!Directory.Exists(resolvedResultsPath))
                    {
                        Directory.CreateDirectory(resolvedResultsPath);
                        Logger.Information("Created Allure results directory: {Path}", resolvedResultsPath);
                    }
                    
                    var resultFiles = Directory.GetFiles(resolvedResultsPath, "*", SearchOption.TopDirectoryOnly);
                    if (resultFiles.Length == 0)
                    {
                        Logger.Warning("No Allure result files found. Searched in the following locations:");
                        Console.WriteLine("[WARNING] No Allure result files found. Searched in the following locations:");
                        foreach (var path in possiblePaths.Distinct().Take(10))
                        {
                            Console.WriteLine($"  - {path}");
                        }
                        Console.WriteLine("[INFO] Allure results are generated automatically by Allure.NUnit during test execution.");
                        Console.WriteLine("[INFO] Make sure tests have been executed successfully.");
                        Console.WriteLine($"[INFO] To generate the HTML report manually, run: allure generate \"{resolvedResultsPath}\" -o Results/AllureReport --clean");
                        return;
                    }
                }
                
                var finalResultFiles = Directory.GetFiles(resolvedResultsPath, "*", SearchOption.TopDirectoryOnly);
                Logger.Information("Found {Count} Allure result file(s) in: {Path}", finalResultFiles.Length, resolvedResultsPath);
                Logger.Information("Found {Count} Allure result file(s) in: {Path}", finalResultFiles.Length, resolvedResultsPath);
                Console.WriteLine($"[INFO] Found {finalResultFiles.Length} Allure result file(s) in: {resolvedResultsPath}");
                
                // Copy categories.json to results directory
                CopyAllureCategories(resolvedResultsPath);
                
                // Try to generate Allure report using Allure CLI
                var allureReportPath = Path.Combine(Path.GetDirectoryName(resolvedResultsPath) ?? resolvedResultsPath, "AllureReport");
                
                // Check if Allure CLI is available
                bool allureCliAvailable = CheckAllureCliAvailable();
                
                if (allureCliAvailable)
                {
                    try
                    {
                        Logger.Information("Generating Allure HTML report...");
                        Console.WriteLine("[INFO] Generating Allure HTML report...");
                        
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = "allure",
                            Arguments = $"generate \"{resolvedResultsPath}\" -o \"{allureReportPath}\" --clean",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        
                        using var process = Process.Start(processStartInfo);
                        if (process != null)
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            var error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            
                            if (process.ExitCode == 0)
                            {
                                Logger.Information("✅ Allure report generated successfully!");
                                Logger.Information("✅ Allure report location: {Path}", allureReportPath);
                                Console.WriteLine("✅ Allure report generated successfully!");
                                Console.WriteLine($"✅ Allure report location: {allureReportPath}");
                                Console.WriteLine($"✅ To open the report, run: allure open \"{allureReportPath}\"");
                                
                                // Check if index.html exists
                                var indexHtml = Path.Combine(allureReportPath, "index.html");
                                if (File.Exists(indexHtml))
                                {
                                    var fileInfo = new FileInfo(indexHtml);
                                    Logger.Information("✅ Report file size: {Size} bytes", fileInfo.Length);
                                    Console.WriteLine($"✅ Report file size: {fileInfo.Length} bytes");
                                }
                            }
                            else
                            {
                                Logger.Error("Failed to generate Allure report. Exit code: {ExitCode}", process.ExitCode);
                                Logger.Error("Error output: {Error}", error);
                                Console.WriteLine($"[ERROR] Failed to generate Allure report. Exit code: {process.ExitCode}");
                                Console.WriteLine($"[ERROR] Error: {error}");
                                Console.WriteLine($"[INFO] You can manually generate the report using:");
                                Console.WriteLine($"       allure generate \"{resolvedResultsPath}\" -o \"{allureReportPath}\" --clean");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to execute Allure CLI: {Error}", ex.Message);
                        Console.WriteLine($"[ERROR] Failed to execute Allure CLI: {ex.Message}");
                        Console.WriteLine($"[INFO] You can manually generate the report using:");
                        Console.WriteLine($"       allure generate \"{resolvedResultsPath}\" -o \"{allureReportPath}\" --clean");
                    }
                }
                else
                {
                    Logger.Warning("Allure CLI is not available. Report generation skipped.");
                    Console.WriteLine("[WARNING] Allure CLI is not available. Report generation skipped.");
                    Console.WriteLine("[INFO] To install Allure CLI:");
                    Console.WriteLine("       Windows: Use Scoop (scoop install allure) or download from https://github.com/allure-framework/allure2/releases");
                    Console.WriteLine("       Or use npm: npm install -g allure-commandline");
                    Console.WriteLine($"[INFO] After installing, manually generate the report using:");
                    Console.WriteLine($"       allure generate \"{resolvedResultsPath}\" -o \"{allureReportPath}\" --clean");
                    Console.WriteLine($"       allure open \"{allureReportPath}\"");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to generate Allure report: {Error}", ex.Message);
                Console.WriteLine($"[ERROR] Failed to generate Allure report: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if Allure CLI is available in the system PATH.
        /// </summary>
        private static bool CheckAllureCliAvailable()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "allure",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(5000); // Wait max 5 seconds
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                // Allure CLI not found
            }
            
            return false;
        }

        /// <summary>
        /// Copies categories.json to the Allure results directory to enable Categories tab in report.
        /// </summary>
        private static void CopyAllureCategories(string resultsPath)
        {
            try
            {
                // Try to find categories.json in multiple locations
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "categories.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "categories.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "QualiFrame.Tests", "Resources", "categories.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Resources", "categories.json")
                };
                
                string? sourceFile = null;
                foreach (var path in possiblePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        sourceFile = fullPath;
                        break;
                    }
                }
                
                if (sourceFile != null)
                {
                    var destFile = Path.Combine(resultsPath, "categories.json");
                    File.Copy(sourceFile, destFile, true);
                    Logger.Information("Copied categories.json to Allure results: {Path}", destFile);
                    Console.WriteLine($"[INFO] Copied categories.json to Allure results: {destFile}");
                }
                else
                {
                    Logger.Warning("categories.json not found. Allure Report Categories tab may be empty.");
                    Console.WriteLine("[WARNING] categories.json not found. Allure Report Categories tab may be empty.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to copy categories.json: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Hook executed before each feature file.
        /// Initializes feature-level reporting.
        /// </summary>
        [BeforeFeature]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            // Ensure ExtentReport is initialized (in case BeforeTestRun didn't execute)
            try
            {
                ExtentReportManager.GetExtentReports();
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to initialize ExtentReport in BeforeFeature: {Error}", ex.Message);
                Console.WriteLine($"[WARNING] Failed to initialize ExtentReport in BeforeFeature: {ex.Message}");
            }
            
            Logger.Information("Starting feature: {FeatureTitle}", featureContext.FeatureInfo.Title);
            ExtentReportManager.CreateFeature(featureContext.FeatureInfo.Title);
        }

        /// <summary>
        /// Hook executed after each feature file.
        /// Performs feature-level cleanup.
        /// </summary>
        [AfterFeature]
        public static void AfterFeature(FeatureContext featureContext)
        {
            Logger.Information("Completed feature: {FeatureTitle}", featureContext.FeatureInfo.Title);
        }

        /// <summary>
        /// Hook executed before each scenario.
        /// Initializes WebDriver, sets up reporting, and configures test context.
        /// Order = 1 ensures this runs before other BeforeScenario hooks.
        /// FeatureContext is injected via dependency injection (recommended Reqnroll 3.0+ pattern).
        /// </summary>
        [BeforeScenario(Order = 1)]
        public static void BeforeScenario(ScenarioContext scenarioContext, FeatureContext featureContext)
        {
            var scenarioTitle = scenarioContext.ScenarioInfo.Title;
            Logger.SetTestId(scenarioTitle);
            Logger.SetBrowser(_config.Browser);
            Logger.SetEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");
            
            Logger.Information("Starting scenario: {ScenarioTitle}", scenarioTitle);
            var featureTitle = featureContext?.FeatureInfo.Title ?? "Unknown Feature";
            ExtentReportManager.CreateScenario(featureTitle, scenarioTitle);
            HealeniumEvidenceContext.Bind(new HealeniumEvidenceScope
            {
                Feature = featureTitle,
                Scenario = scenarioTitle
            });
            QhareHealingContext.Bind(new QhareHealingScope
            {
                Feature = featureTitle,
                TestId = scenarioTitle,
                ExecutionId = Guid.NewGuid().ToString("N")
            });
            
            // Allure test case setup
            if (_config.AllureReportEnabled)
            {
                try
                {
                    Logger.Information("Allure reporting is ENABLED - initializing test case for: {ScenarioTitle}", scenarioTitle);
                    Console.WriteLine($"[INFO] Allure reporting is ENABLED - initializing test case for: {scenarioTitle}");
                    
                    // Create a new test result for this scenario
                    var testUuid = Guid.NewGuid().ToString();
                    var testResult = new TestResult
                    {
                        uuid = testUuid,
                        name = scenarioTitle,
                        fullName = $"{featureTitle}.{scenarioTitle}",
                        historyId = scenarioTitle.GetHashCode().ToString(),
                        status = Status.none,
                        start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    
                    // Add feature information
                    testResult.labels.Add(new Label { name = "feature", value = featureTitle });
                    
                    // Add tags as labels
                    foreach (var tag in scenarioContext.ScenarioInfo.Tags)
                    {
                        testResult.labels.Add(new Label { name = "tag", value = tag });
                    }
                    
                    // Start the test case
                    AllureLifecycle.Instance.StartTestCase(testResult);
                    
                    // Store test UUID and feature title in scenario context for later use
                    scenarioContext["AllureTestUuid"] = testUuid;
                    scenarioContext["AllureFeatureTitle"] = featureTitle;
                    
                    Logger.Information("✅ Allure test case started: {Uuid} - {Name}", testUuid, scenarioTitle);
                    Console.WriteLine($"[INFO] ✅ Allure test case started: {testUuid} - {scenarioTitle}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "❌ Failed to start Allure test case: {Error}", ex.Message);
                    Console.WriteLine($"[ERROR] ❌ Failed to start Allure test case: {ex.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Logger.Warning("Allure reporting is DISABLED - skipping test case initialization");
                Console.WriteLine("[WARNING] Allure reporting is DISABLED - skipping test case initialization");
            }
            
            // Check tags to decide which driver stack to use
            var tags = scenarioContext.ScenarioInfo.Tags;
            bool isApiTest = tags.Contains("API") || tags.Any(t => t.Contains("API", StringComparison.OrdinalIgnoreCase));
            bool isMobileTest = tags.Contains("Mobile") || tags.Any(t => t.Contains("Mobile", StringComparison.OrdinalIgnoreCase));
            
            if (isApiTest)
            {
                Logger.Information("API test detected - skipping WebDriver initialization for scenario: {ScenarioTitle}", scenarioTitle);
                // For API tests, we don't need WebDriver, so we skip initialization
                return;
            }

            // Treat both generic Desktop scenarios and Calculator-specific scenarios as desktop automation.
            bool isDesktopTest =
                tags.Contains("Desktop") ||
                tags.Any(t => t.Contains("Desktop", StringComparison.OrdinalIgnoreCase)) ||
                tags.Contains("Calculator") ||
                tags.Any(t => t.Contains("Calculator", StringComparison.OrdinalIgnoreCase));

            if (isDesktopTest)
            {
                Logger.Information("Desktop test detected - skipping WebDriver initialization for scenario: {ScenarioTitle}", scenarioTitle);
                // For Desktop tests, FlaUI sessions are initialized in DesktopHooks / DesktopSessionFactory.
                return;
            }

            if (isMobileTest)
            {
                Logger.Information("Mobile test detected - skipping WebDriver initialization for scenario: {ScenarioTitle}", scenarioTitle);
                // Appium sessions are created through AppiumDriverFactory in MobileSteps.
                return;
            }
            
            // For non-API tests, initialize WebDriver using EnhancedWebDriverFactory
            // Step definitions will get WebDriver from EnhancedWebDriverFactory.GetDriver() via BasePage
            try
            {
                // Clean up any existing driver first to prevent multiple instances
                try
                {
                    EnhancedWebDriverFactory.QuitDriver();
                }
                catch
                {
                    // Ignore cleanup errors if no driver exists
                }
                
                var driver = EnhancedWebDriverFactory.CreateDriver();
                if (driver == null)
                {
                    throw new InvalidOperationException("Failed to initialize WebDriver");
                }
                
                // IMPORTANT: Set the driver in the factory's current driver value
                // This ensures GetDriver() returns the same instance instead of creating a new one
                // This allows step definitions to access the driver via BasePage.Driver property
                EnhancedWebDriverFactory.SetCurrentDriver(driver);
                
                Logger.Information("WebDriver initialized successfully for scenario: {ScenarioTitle}", scenarioTitle);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize WebDriver for scenario: {ScenarioTitle}", scenarioTitle);
                try
                {
                    EnhancedWebDriverFactory.QuitDriver();
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Hook executed after each scenario.
        /// Handles failure artifacts (screenshots), updates reporting, and cleans up WebDriver.
        /// </summary>
        [AfterScenario]
        public static void AfterScenario(ScenarioContext scenarioContext)
        {
            var scenarioTitle = scenarioContext.ScenarioInfo.Title;
            
            // Allure test case completion
            if (_config.AllureReportEnabled)
            {
                try
                {
                    Logger.Information("Completing Allure test case for: {ScenarioTitle}", scenarioTitle);
                    Console.WriteLine($"[INFO] Completing Allure test case for: {scenarioTitle}");
                    
                    if (scenarioContext.TryGetValue("AllureTestUuid", out var testUuidObj) && testUuidObj is string testUuid)
                    {
                        Logger.Information("Found Allure test UUID: {Uuid}", testUuid);
                        Console.WriteLine($"[INFO] Found Allure test UUID: {testUuid}");

                        // Update the existing test case in Allure lifecycle and capture final state
                        TestResult? finalResult = null;
                        var scenarioHeals = QhareHealingContext.Current?.ScenarioHeals
                            ?? (IReadOnlyList<QhareHealRecord>)Array.Empty<QhareHealRecord>();
                        AllureLifecycle.Instance.UpdateTestCase(tc =>
                        {
                            if (tc.uuid == testUuid)
                            {
                                // Set final status
                                tc.status = scenarioContext.TestError != null ? Status.failed : Status.passed;

                                // Mark stage as finished
                                tc.stage = Stage.finished;

                                // Set stop time
                                tc.stop = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                                // Populate error details if scenario failed
                                if (scenarioContext.TestError != null)
                                {
                                    tc.statusDetails ??= new StatusDetails();
                                    tc.statusDetails.message = scenarioContext.TestError.Message;
                                    tc.statusDetails.trace = scenarioContext.TestError.StackTrace ?? "";
                                }

                                // Ensure we have a name for the test
                                if (string.IsNullOrEmpty(tc.name))
                                {
                                    tc.name = scenarioTitle;
                                }

                                // Ensure we have a fullName
                                if (string.IsNullOrEmpty(tc.fullName))
                                {
                                    var fullName = scenarioTitle;
                                    if (scenarioContext.TryGetValue("AllureFeatureTitle", out var featureTitleObj2) && featureTitleObj2 is string featureTitle2)
                                    {
                                        fullName = $"{featureTitle2}.{scenarioTitle}";
                                    }
                                    tc.fullName = fullName;
                                }

                                if (scenarioHeals.Count > 0)
                                {
                                    QhareAllureEvidence.AnnotateTestCase(tc, scenarioHeals);
                                    if (scenarioContext.TestError == null &&
                                        !tc.name.Contains("[QHARE healed]", StringComparison.OrdinalIgnoreCase))
                                    {
                                        tc.name = $"{tc.name} [QHARE healed]";
                                    }
                                }

                                // Capture final state for manual serialization
                                finalResult = tc;
                            }
                        });
                        
                        // Stop the test case - this will write the result to file
                        // StopTestCase() without parameters stops the current test case
                        AllureLifecycle.Instance.StopTestCase();
                        
                        // Explicitly write the test result to ensure it's saved to disk
                        // Get the results directory using helper method (checks env var first, then config)
                        var resultsDir = GetAllureResultsDirectory();
                        Logger.Information("Resolved Allure results directory: {Path}", resultsDir);
                        Console.WriteLine($"[INFO] Resolved Allure results directory: {resultsDir}");
                        
                        // Ensure directory exists
                        if (!Directory.Exists(resultsDir))
                        {
                            Directory.CreateDirectory(resultsDir);
                            Logger.Information("Created Allure results directory: {Path}", resultsDir);
                            Console.WriteLine($"[INFO] Created Allure results directory: {resultsDir}");
                        }
                        
                        // Manually write the test result JSON file in Allure format
                        try
                        {
                            // If for some reason we didn't capture the final result above, log and bail out
                            if (finalResult == null)
                            {
                                Logger.Warning("Allure finalResult is null for test UUID: {Uuid}. Skipping manual result write.", testUuid);
                                Console.WriteLine($"[WARNING] Allure finalResult is null for test UUID: {testUuid}. Skipping manual result write.");
                                return;
                            }

                            // Ensure we have all required fields for Allure format
                            if (finalResult.start == 0)
                            {
                                finalResult.start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000; // Default to 1 second ago if not set
                            }
                            if (finalResult.stop == 0)
                            {
                                finalResult.stop = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            }

                            // Ensure stage is finished
                            finalResult.stage = Stage.finished;

                            // Ensure status is set
                            if (finalResult.status == Status.none)
                            {
                                finalResult.status = scenarioContext.TestError != null ? Status.failed : Status.passed;
                            }

                            var resultFile = Path.Combine(resultsDir, $"{testUuid}-result.json");
                            
                            // Use Newtonsoft.Json for better compatibility with Allure format
                            // IMPORTANT: Serialize enums as strings so Allure CLI understands status/stage properly
                            var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
                            {
                                Formatting = Newtonsoft.Json.Formatting.Indented,
                                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                            };
                            jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

                            var resultJson = Newtonsoft.Json.JsonConvert.SerializeObject(finalResult, jsonSettings);
                            
                            // Write the file
                            File.WriteAllText(resultFile, resultJson, System.Text.Encoding.UTF8);
                            
                            Logger.Information("✅ Manually wrote Allure test result to: {File}", resultFile);
                            Console.WriteLine($"[INFO] ✅ Allure result written to: {resultFile}");
                            
                            // Verify file was written
                            if (File.Exists(resultFile))
                            {
                                var fileInfo = new FileInfo(resultFile);
                                Logger.Information("✅ Verified result file exists: {File} ({Size} bytes)", resultFile, fileInfo.Length);
                                Console.WriteLine($"[INFO] ✅ Verified result file exists: {resultFile} ({fileInfo.Length} bytes)");
                                
                                // Also write a container file (Allure expects both)
                                try
                                {
                                    // Get feature title from scenario context
                                    var containerFeatureTitle = "Test Feature";
                                    if (scenarioContext.TryGetValue("AllureFeatureTitle", out var featureTitleObj) && featureTitleObj is string storedFeatureTitle)
                                    {
                                        containerFeatureTitle = storedFeatureTitle;
                                    }
                                    
                                    var containerUuid = Guid.NewGuid().ToString();
                                    var containerFile = Path.Combine(resultsDir, $"{containerUuid}-container.json");
                                    var container = new
                                    {
                                        uuid = containerUuid,
                                        name = containerFeatureTitle,
                                        children = new[] { testUuid },
                                        befores = new object[0],
                                        afters = new object[0]
                                    };
                                    var containerJson = Newtonsoft.Json.JsonConvert.SerializeObject(container, Newtonsoft.Json.Formatting.Indented);
                                    File.WriteAllText(containerFile, containerJson, System.Text.Encoding.UTF8);
                                    Logger.Information("✅ Wrote Allure container file: {File}", containerFile);
                                    Console.WriteLine($"[INFO] ✅ Wrote Allure container file: {containerFile}");
                                }
                                catch (Exception containerEx)
                                {
                                    Logger.Warning("Failed to write container file: {Error}", containerEx.Message);
                                    Console.WriteLine($"[WARNING] Failed to write container file: {containerEx.Message}");
                                }
                            }
                            else
                            {
                                Logger.Error("❌ Result file was not created: {File}", resultFile);
                                Console.WriteLine($"[ERROR] ❌ Result file was not created: {resultFile}");
                            }
                        }
                        catch (Exception writeEx)
                        {
                            Logger.Error(writeEx, "❌ Failed to manually write Allure result file: {Error}", writeEx.Message);
                            Console.WriteLine($"[ERROR] ❌ Failed to manually write Allure result file: {writeEx.Message}");
                            Console.WriteLine($"[ERROR] Stack trace: {writeEx.StackTrace}");
                        }
                        
                        // Log final status if we have the final result
                        if (finalResult != null)
                        {
                            Logger.Information("Allure test case completed: {Uuid} - {Status}", testUuid, finalResult.status);
                            Console.WriteLine($"[INFO] Allure test case completed: {testUuid} - {finalResult.status}");
                        }
                    }
                    else
                    {
                        Logger.Warning("⚠️ Allure test UUID not found in scenario context for: {ScenarioTitle}", scenarioTitle);
                        Console.WriteLine($"[WARNING] ⚠️ Allure test UUID not found in scenario context for: {scenarioTitle}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "❌ Failed to complete Allure test case: {Error}", ex.Message);
                    Console.WriteLine($"[ERROR] ❌ Failed to complete Allure test case: {ex.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Logger.Warning("Allure reporting is DISABLED - skipping test case completion");
                Console.WriteLine("[WARNING] Allure reporting is DISABLED - skipping test case completion");
            }
            
            // Determine scenario status and update ExtentReport accordingly
            var scenarioHealsForExtent = QhareHealingContext.Current?.ScenarioHeals
                ?? (IReadOnlyList<QhareHealRecord>)Array.Empty<QhareHealRecord>();
            if (scenarioHealsForExtent.Count > 0)
            {
                QhareExtentEvidence.AnnotateScenario(scenarioHealsForExtent);
            }

            if (scenarioContext.TestError != null)
            {
                Logger.Error(scenarioContext.TestError, "Scenario failed: {ScenarioTitle}", scenarioTitle);
                var failMessage = scenarioHealsForExtent.Count > 0
                    ? $"Scenario failed: {scenarioContext.TestError.Message} ({scenarioHealsForExtent.Count} QHARE heal(s) earlier in the scenario)"
                    : $"Scenario failed: {scenarioContext.TestError.Message}";
                ExtentReportManager.MarkScenarioAsFailed(failMessage);
                
                // Try to capture screenshot for failed scenarios (non-API tests)
                var tags = scenarioContext.ScenarioInfo.Tags;
                bool isApiTest = tags.Contains("API") || tags.Any(t => t.Contains("API", StringComparison.OrdinalIgnoreCase));
                bool isMobileTest = tags.Contains("Mobile") || tags.Any(t => t.Contains("Mobile", StringComparison.OrdinalIgnoreCase));
                bool isDesktopTest = tags.Contains("Desktop") || tags.Any(t => t.Contains("Desktop", StringComparison.OrdinalIgnoreCase)) || tags.Contains("Calculator") || tags.Any(t => t.Contains("Calculator", StringComparison.OrdinalIgnoreCase));
                
                if (!isApiTest && !isMobileTest && !isDesktopTest)
                {
                    try
                    {
                        // Try to get driver from factory (may not exist if test failed early)
                        var driver = EnhancedWebDriverFactory.GetDriver();
                        if (driver != null)
                        {
                            try
                            {
                                var screenshotName = $"{scenarioTitle}_{DateTime.Now:yyyyMMdd_HHmmss}";
                                var screenshotPath = EnhancedWebDriverFactory.TakeScreenshot(driver, screenshotName);
                                
                                if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                                {
                                    ExtentReportManager.AddScreenshot(screenshotPath, "Failure Screenshot");
                                    
                                    // Add screenshot to Allure
                                    if (_config.AllureReportEnabled)
                                    {
                                        try
                                        {
                                            var screenshotBytes = File.ReadAllBytes(screenshotPath);
                                            AllureApi.AddAttachment("Failure Screenshot", "image/png", screenshotBytes);
                                            Logger.Debug("Screenshot added to Allure");
                                        }
                                        catch (Exception allureEx)
                                        {
                                            Logger.Warning("Failed to add screenshot to Allure: {Error}", allureEx.Message);
                                        }
                                    }
                                }
                            }
                            catch (Exception screenshotEx)
                            {
                                Logger.Warning("Failed to take screenshot for failed scenario: {Error}", screenshotEx.Message);
                            }
                        }
                    }
                    catch
                    {
                        // Driver not available, skip screenshot
                        Logger.Debug("Driver not available for screenshot");
                    }
                }
            }
            else if (scenarioHealsForExtent.Count > 0)
            {
                Logger.Information(
                    "Scenario passed with QHARE self-healing ({Count} locator(s)): {ScenarioTitle}",
                    scenarioHealsForExtent.Count,
                    scenarioTitle);
                ExtentReportManager.MarkScenarioAsPassed(
                    $"Scenario passed successfully ({scenarioHealsForExtent.Count} QHARE heal(s))");
            }
            else
            {
                Logger.Information("Scenario passed successfully: {ScenarioTitle}", scenarioTitle);
                ExtentReportManager.MarkScenarioAsPassed("Scenario passed successfully");
            }
            
            // Cleanup WebDriver for non-API and non-Mobile tests
            var scenarioTags = scenarioContext.ScenarioInfo.Tags;
            bool isApiTestScenario = scenarioTags.Contains("API") || scenarioTags.Any(t => t.Contains("API", StringComparison.OrdinalIgnoreCase));
            bool isMobileTestScenario = scenarioTags.Contains("Mobile") || scenarioTags.Any(t => t.Contains("Mobile", StringComparison.OrdinalIgnoreCase));
            bool isDesktopTestScenario = scenarioTags.Contains("Desktop") || scenarioTags.Any(t => t.Contains("Desktop", StringComparison.OrdinalIgnoreCase));
            
            if (isMobileTestScenario)
            {
                try
                {
                    AppiumDriverFactory.QuitDriver();
                    Logger.Debug("Appium driver cleanup completed for scenario: {ScenarioTitle}", scenarioTitle);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Error during Appium driver cleanup: {Error}", ex.Message);
                }
            }
            else if (!isApiTestScenario && !isDesktopTestScenario)
            {
                try
                {
                    //EnhancedWebDriverFactory.QuitDriver();
                    EnhancedWebDriverFactory.CloseDriver();
                    Logger.Debug("Driver cleanup completed for scenario: {ScenarioTitle}", scenarioTitle);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Error during driver cleanup: {Error}", ex.Message);
                }
            }
            HealeniumEvidenceContext.Unbind();
            QhareHealingContext.Unbind();
        }

        /// <summary>
        /// Hook executed before each step.
        /// Logs step execution and initializes step-level reporting.
        /// </summary>
        [BeforeStep]
        public static void BeforeStep(ScenarioContext scenarioContext)
        {
            var stepInfo = scenarioContext.StepContext.StepInfo;
            var keyword = stepInfo.StepDefinitionType.ToString();
            var stepDisplayName = $"{keyword} {stepInfo.Text}";
            Logger.Debug("Executing step: {StepText}", stepDisplayName);
            ExtentReportManager.CreateStep(stepDisplayName);
            HealeniumEvidenceContext.Current?.BeginStep(stepInfo.Text);
            QhareHealingContext.Current?.BeginStep(stepInfo.Text);
            
            // Allure step logging
            if (_config.AllureReportEnabled)
            {
                try
                {
                    AllureLifecycle.Instance.StartStep(new StepResult
                    {
                        name = stepDisplayName
                    });
                }
                catch (Exception ex)
                {
                    Logger.Debug("Failed to start Allure step: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Hook executed after each step.
        /// Updates step status in reporting and captures screenshots if enabled.
        /// </summary>
        [AfterStep]
        public static void AfterStep(ScenarioContext scenarioContext)
        {
            var stepInfo = scenarioContext.StepContext.StepInfo;
            var stepPassed = scenarioContext.TestError == null;
            var healer = SelfHealingProviderResolver.Resolve(_config);
            IReadOnlyList<QhareHealRecord> stepHeals = Array.Empty<QhareHealRecord>();
            if (healer == SelfHealingProvider.Qhare)
            {
                try
                {
                    // Capture heals for Allure before telemetry commit clears step caches.
                    stepHeals = QhareHealingContext.ConsumeStepHealsForReporting(stepPassed);
                    QhareHealingContext.CommitBoundStep(stepPassed);
                }
                catch (Exception ex)
                {
                    Logger.Debug("QHARE telemetry commit skipped: {Error}", ex.Message);
                }
            }
            else if (healer == SelfHealingProvider.Healenium)
            {
                try
                {
                    HealeniumEvidenceStore.ForConfiguration(_config.GetHealeniumConfiguration())
                        .CommitBoundStep(stepPassed);
                }
                catch (Exception ex)
                {
                    Logger.Debug("Healenium evidence commit skipped: {Error}", ex.Message);
                }
            }
            
            // Allure step completion
            if (_config.AllureReportEnabled)
            {
                try
                {
                    // Check if step has an error
                    if (scenarioContext.TestError != null)
                    {
                        AllureLifecycle.Instance.UpdateStep(step =>
                        {
                            step.status = Status.failed;
                            step.statusDetails = new StatusDetails
                            {
                                message = scenarioContext.TestError.Message,
                                trace = scenarioContext.TestError.StackTrace
                            };
                        });
                    }
                    else
                    {
                        AllureLifecycle.Instance.UpdateStep(step =>
                        {
                            step.status = Status.passed;
                        });
                    }

                    if (stepHeals.Count > 0)
                    {
                        QhareAllureEvidence.PublishPassingStepHeals(stepHeals);
                        Logger.Information(
                            "Allure annotated step with {Count} QHARE heal(s): {Step}",
                            stepHeals.Count,
                            stepInfo.Text);
                    }
                    
                    AllureLifecycle.Instance.StopStep();
                }
                catch (Exception ex)
                {
                    Logger.Debug("Failed to complete Allure step: {Error}", ex.Message);
                }
            }
            else if (stepHeals.Count > 0)
            {
                Logger.Information(
                    "QHARE healed {Count} locator(s) on step '{Step}' (Allure reporting disabled)",
                    stepHeals.Count,
                    stepInfo.Text);
            }

            // Extent: always record the step outcome; attach QHARE heal details when present (pass or fail).
            if (stepHeals.Count > 0)
            {
                QhareExtentEvidence.PublishStepHeals(stepHeals, stepPassed);
                Logger.Information(
                    "Extent annotated step with {Count} QHARE heal(s): {Step}",
                    stepHeals.Count,
                    stepInfo.Text);
            }

            if (scenarioContext.TestError != null)
            {
                Logger.Error(scenarioContext.TestError, "Step failed: {StepText}", stepInfo.Text);
                ExtentReportManager.LogFail($"Step failed: {scenarioContext.TestError.Message}");
            }
            else if (stepHeals.Count == 0)
            {
                Logger.Debug("Step completed successfully: {StepText}", stepInfo.Text);
                ExtentReportManager.LogPass("Step completed successfully");
            }
            
            // Capture screenshot for each step if screenshots are enabled and this is not an API test
            if (_config.ScreenshotsEnabled)
            {
                var tags = scenarioContext.ScenarioInfo.Tags;
                bool isApiTest = tags.Contains("API") || tags.Any(t => t.Contains("API", StringComparison.OrdinalIgnoreCase));
                bool isMobileTest = tags.Contains("Mobile") || tags.Any(t => t.Contains("Mobile", StringComparison.OrdinalIgnoreCase));
                bool isDesktopTest = tags.Contains("Desktop") || tags.Any(t => t.Contains("Desktop", StringComparison.OrdinalIgnoreCase)) || tags.Contains("Calculator") || tags.Any(t => t.Contains("Calculator", StringComparison.OrdinalIgnoreCase));
                
                if (!isApiTest && !isMobileTest && !isDesktopTest)
                {
                    try
                    {
                        // Try to get driver from factory
                        var driver = EnhancedWebDriverFactory.GetDriver();
                        
                        if (driver != null && DriverHealthChecker.IsHealthy(driver))
                        {
                            try
                            {
                                // Create a sanitized step name for the screenshot file
                                var sanitizedStepName = stepInfo.Text
                                    .Replace(" ", "_")
                                    .Replace("\"", "")
                                    .Replace("'", "")
                                    .Replace("/", "_")
                                    .Replace("\\", "_")
                                    .Replace(":", "_")
                                    .Replace("*", "_")
                                    .Replace("?", "_")
                                    .Replace("<", "_")
                                    .Replace(">", "_")
                                    .Replace("|", "_");
                                
                                // Limit length to avoid file system issues
                                if (sanitizedStepName.Length > 100)
                                {
                                    sanitizedStepName = sanitizedStepName.Substring(0, 100);
                                }
                                
                                // Use sanitized step name without timestamp (TakeScreenshot adds its own timestamp)
                                var screenshotPath = EnhancedWebDriverFactory.TakeScreenshot(driver, sanitizedStepName);
                                
                                // Add screenshot to ExtentReport if available
                                if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                                {
                                    var stepDescription = stepInfo.Text.Length > 50 
                                        ? stepInfo.Text.Substring(0, 50) + "..." 
                                        : stepInfo.Text;
                                    ExtentReportManager.AddScreenshot(screenshotPath, $"Step: {stepDescription}");
                                    Logger.Debug("Screenshot captured and added to ExtentReport for step: {StepText}", stepInfo.Text);
                                }
                            }
                            catch (Exception screenshotEx)
                            {
                                // Don't fail the test if screenshot capture fails
                                Logger.Debug("Failed to capture screenshot for step: {StepText}, Error: {Error}", stepInfo.Text, screenshotEx.Message);
                            }
                        }
                    }
                    catch
                    {
                        // Driver not available, skip screenshot
                        Logger.Debug("Driver not available for step screenshot");
                    }
                }
            }
        }
        
        [BeforeScenario]
        public void BeforeScenario(ScenarioContext scenarioContext)
        {
            try
            {
                // Enrich Allure report with custom labels based on tags
                EnrichAllureReport(scenarioContext);
            
                // Setup feature-specific configuration if needed
                // Feature context logic removed as _featureContext is not available here
            }
            catch (Exception ex)
            {
                Logger.Warning("Error during BeforeScenario hook: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Enriches the Allure report by mapping Reqnroll tags to Allure labels (Epic, Severity, etc.)
        /// </summary>
        private void EnrichAllureReport(ScenarioContext scenarioContext)
        {
            try
            {
                var tags = scenarioContext.ScenarioInfo.Tags;
                if (tags == null || !tags.Any()) return;

                AllureLifecycle.Instance.UpdateTestCase(testCase =>
                {
                    foreach (var tag in tags)
                    {
                        // Map Priority tags to Severity
                        if (tag.StartsWith("P1", StringComparison.OrdinalIgnoreCase))
                        {
                            testCase.labels.Add(Label.Severity(SeverityLevel.critical));
                        }
                        else if (tag.StartsWith("P2", StringComparison.OrdinalIgnoreCase))
                        {
                            testCase.labels.Add(Label.Severity(SeverityLevel.normal));
                        }
                        else if (tag.StartsWith("P3", StringComparison.OrdinalIgnoreCase))
                        {
                            testCase.labels.Add(Label.Severity(SeverityLevel.minor));
                        }

                        // Map Feature tags to Epics
                        if (tag.Equals("WebDemo", StringComparison.OrdinalIgnoreCase) || 
                            tag.Equals("Web", StringComparison.OrdinalIgnoreCase))
                        {
                            testCase.labels.Add(Label.Epic("Web Application"));
                        }
                        else if (tag.Equals("API", StringComparison.OrdinalIgnoreCase))
                        {
                            testCase.labels.Add(Label.Epic("API Services"));
                        }
                        else if (tag.Equals("Mobile", StringComparison.OrdinalIgnoreCase))
                        {
                            testCase.labels.Add(Label.Epic("Mobile Application"));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to enrich Allure report: {Error}", ex.Message);
            }
        }
    }
}

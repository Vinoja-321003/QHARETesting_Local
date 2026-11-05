using System.Diagnostics;

namespace QualiFrame.Runner.Core
{
    /// <summary>
    /// Reqnroll-BDD Runner that executes tests via dotnet test CLI.
    /// Utilizes existing Reqnroll Hooks in QualiFrame.Tests.Hooks for full lifecycle management.
    /// Maps Reqnroll tags (@tag) to NUnit Categories (Category=tag) for filtering.
    /// No standalone/custom runner logic - delegates all execution to dotnet test.
    /// </summary>
    public class TestRunner
    {
        private readonly RunnerConfiguration _config = RunnerConfiguration.Instance;
        private readonly TestRunnerOptions _options;

        public TestRunner() : this(new TestRunnerOptions())
        {
        }

        public TestRunner(TestRunnerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string BuildDotNetTestArguments()
        {
            var args = new List<string>();

            if (!string.IsNullOrEmpty(_options.ProjectPath))
            {
                args.Add(_options.ProjectPath);
            }

            args.Add("--configuration");
            args.Add(_options.Configuration ?? "Debug");

            if (_options.EffectiveParallelExecution)
            {
                args.Add("--parallel");
                RunnerLog.Information("Parallel execution enabled (MaxParallelTests: {0})", _config.MaxParallelTests);
            }

            if (!string.IsNullOrEmpty(_options.Filter))
            {
                args.Add("--filter");
                args.Add($"\"{_options.Filter}\"");
                RunnerLog.Debug("Test filter applied: {0}", _options.Filter ?? "");
            }

            var verbosityArg = _options.Verbosity switch
            {
                TestRunnerVerbosity.Quiet => "quiet",
                TestRunnerVerbosity.Normal => "normal",
                TestRunnerVerbosity.Detailed => "detailed",
                TestRunnerVerbosity.Diagnostic => "diagnostic",
                _ => "normal"
            };
            args.Add("--logger");
            args.Add($"\"console;verbosity={verbosityArg}\"");

            foreach (var logger in _options.Loggers)
            {
                args.Add("--logger");
                args.Add($"\"{logger}\"");
            }

            if (_options.CollectCoverage)
            {
                args.Add("/p:CollectCoverage=true");
                args.Add($"/p:CoverletOutputFormat={_options.CoverageFormat}");
            }

            if (!_options.BuildBeforeRun)
            {
                args.Add("--no-build");
            }

            if (!_options.RestorePackages)
            {
                args.Add("--no-restore");
            }

            return string.Join(" ", args);
        }

        public static string BuildScenarioFilter(string scenarioName)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                throw new ArgumentException("Scenario name cannot be null or empty", nameof(scenarioName));
            }

            return $"FullyQualifiedName~{scenarioName}";
        }

        public static string BuildFeatureFilter(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name cannot be null or empty", nameof(featureName));
            }

            return $"FullyQualifiedName~{featureName}";
        }

        public static string BuildTagFilter(string[] tags, bool useAndOperator = true)
        {
            if (tags == null || tags.Length == 0)
            {
                throw new ArgumentException("Tags cannot be null or empty", nameof(tags));
            }

            var cleanedTags = tags.Select(tag => tag.TrimStart('@').Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray();

            if (cleanedTags.Length == 0)
            {
                throw new ArgumentException("No valid tags found after processing", nameof(tags));
            }

            var operatorChar = useAndOperator ? "&" : "|";
            var categoryFilters = cleanedTags.Select(tag => $"Category={tag}").ToArray();

            return string.Join(operatorChar, categoryFilters);
        }

        public static string BuildExcludeTagFilter(string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                throw new ArgumentException("Tags cannot be null or empty", nameof(tags));
            }

            var cleanedTags = tags.Select(tag => tag.TrimStart('@').Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray();

            if (cleanedTags.Length == 0)
            {
                throw new ArgumentException("No valid tags found after processing", nameof(tags));
            }

            var excludeFilters = cleanedTags.Select(tag => $"Category!={tag}").ToArray();
            return string.Join("&", excludeFilters);
        }

        public static string BuildCombinedFilter(
            string? scenarioName = null,
            string? featureName = null,
            string[]? includeTags = null,
            string[]? excludeTags = null,
            bool tagOperator = true)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(scenarioName))
            {
                filters.Add(BuildScenarioFilter(scenarioName));
            }

            if (!string.IsNullOrWhiteSpace(featureName))
            {
                filters.Add(BuildFeatureFilter(featureName));
            }

            if (includeTags != null && includeTags.Length > 0)
            {
                filters.Add(BuildTagFilter(includeTags, tagOperator));
            }

            if (excludeTags != null && excludeTags.Length > 0)
            {
                filters.Add(BuildExcludeTagFilter(excludeTags));
            }

            if (filters.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("&", filters);
        }

        public int Execute()
        {
            RunnerLog.Information("═══════════════════════════════════════════════════════════");
            RunnerLog.Information("Starting test execution via dotnet test");
            RunnerLog.Information("Test Runner Configuration:");
            RunnerLog.Information("  - Configuration: {0}", _options.Configuration ?? "Debug");
            RunnerLog.Information("  - Parallel Execution: {0}", _options.EffectiveParallelExecution);
            if (_options.EffectiveParallelExecution)
            {
                RunnerLog.Information("  - Max Parallel Tests: {0}", _config.MaxParallelTests);
            }
            if (!string.IsNullOrEmpty(_options.Filter))
            {
                RunnerLog.Information("  - Filter: {0}", _options.Filter ?? "");
            }
            RunnerLog.Information("  - Verbosity: {0}", _options.Verbosity?.ToString() ?? "Normal");
            RunnerLog.Information("═══════════════════════════════════════════════════════════");

            if (_options.CleanBuild)
            {
                RunnerLog.Information("Performing clean build...");
                var cleanExitCode = ExecuteCommand("dotnet", "clean");
                if (cleanExitCode != 0)
                {
                    RunnerLog.Warning("Clean build returned non-zero exit code: {0}", cleanExitCode);
                }
            }

            if (_options.RestorePackages)
            {
                RunnerLog.Information("Restoring packages...");
                var restoreExitCode = ExecuteCommand("dotnet", "restore");
                if (restoreExitCode != 0)
                {
                    RunnerLog.Warning("Package restore returned non-zero exit code: {0}", restoreExitCode);
                }
            }

            var arguments = BuildDotNetTestArguments();
            var command = "dotnet test";

            RunnerLog.Information("Executing: {0} {1}", command, arguments);
            Console.WriteLine($"[INFO] Executing: {command} {arguments}");

            var exitCode = ExecuteCommand("dotnet", $"test {arguments}");

            if (_options.ShowSummary)
            {
                RunnerLog.Information("═══════════════════════════════════════════════════════════");
                if (exitCode == 0)
                {
                    RunnerLog.Information("✅ Test execution completed successfully");
                    Console.WriteLine("✅ Test execution completed successfully");
                }
                else
                {
                    RunnerLog.Warning("❌ Test execution completed with errors (Exit Code: {0})", exitCode);
                    Console.WriteLine($"❌ Test execution completed with errors (Exit Code: {exitCode})");
                }

                RunnerLog.Information("Reports available at:");
                RunnerLog.Information("  - Extent Report: {0}", _config.ExtentReportPath ?? "");
                RunnerLog.Information("  - Allure Results: {0}", _config.AllureResultsPath ?? "");
                RunnerLog.Information("  - Screenshots: {0}", _config.ScreenshotsPath ?? "");
                RunnerLog.Information("  - Logs: {0}", _config.LogPath ?? "");
                RunnerLog.Information("═══════════════════════════════════════════════════════════");
            }

            return exitCode;
        }

        private int ExecuteCommand(string command, string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    RunnerLog.Error("Failed to start process: {0} {1}", command, arguments);
                    return -1;
                }

                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.Error.WriteLine(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                RunnerLog.Error(ex, "Failed to execute command: {0} {1}", command, arguments);
                return -1;
            }
        }

        public static string GetDefaultProjectPath()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);

            while (dir != null && dir.Exists)
            {
                var csprojFiles = dir.GetFiles("QualiFrame.Tests.csproj", SearchOption.TopDirectoryOnly);
                if (csprojFiles.Length > 0)
                {
                    return csprojFiles[0].FullName;
                }

                dir = dir.Parent;
            }

            return "src/QualiFrame.Tests/QualiFrame.Tests.csproj";
        }
    }
}

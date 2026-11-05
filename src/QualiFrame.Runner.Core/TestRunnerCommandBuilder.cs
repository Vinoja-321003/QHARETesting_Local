using System.Text;

namespace QualiFrame.Runner.Core
{
    /// <summary>
    /// Builds dotnet test commands for executing Reqnroll-BDD tests.
    /// Delegates to NUnit/VSTest CLI via dotnet test without introducing standalone runner logic.
    /// </summary>
    public static class TestRunnerCommandBuilder
    {
        private static readonly RunnerConfiguration _config = RunnerConfiguration.Instance;
        private const string ProjectPath = "src/QualiFrame.Tests/QualiFrame.Tests.csproj";

        public static string BuildAllTestsCommand(TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();
            return BuildCommand(options);
        }

        public static string BuildFeatureCommand(string featureName, TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();

            var filter = $"FullyQualifiedName~{EscapeFilterValue(featureName)}";
            options.Filter = string.IsNullOrEmpty(options.Filter)
                ? filter
                : $"{options.Filter}&{filter}";

            return BuildCommand(options);
        }

        public static string BuildScenarioCommand(string scenarioName, TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();

            var filter = $"FullyQualifiedName~{EscapeFilterValue(scenarioName)}";
            options.Filter = string.IsNullOrEmpty(options.Filter)
                ? filter
                : $"{options.Filter}&{filter}";

            return BuildCommand(options);
        }

        public static string BuildTagsCommand(IEnumerable<string> tags, TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();

            var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                              .Select(t => t.TrimStart('@').Trim())
                              .Where(t => !string.IsNullOrEmpty(t))
                              .ToList();

            if (tagList.Count == 0)
            {
                throw new ArgumentException("At least one tag must be provided.", nameof(tags));
            }

            var categoryFilter = string.Join("&", tagList.Select(tag => $"Category={EscapeFilterValue(tag)}"));

            options.Filter = string.IsNullOrEmpty(options.Filter)
                ? categoryFilter
                : $"{options.Filter}&{categoryFilter}";

            return BuildCommand(options);
        }

        public static string BuildTagsOrCommand(IEnumerable<string> tags, TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();

            var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                              .Select(t => t.TrimStart('@').Trim())
                              .Where(t => !string.IsNullOrEmpty(t))
                              .ToList();

            if (tagList.Count == 0)
            {
                throw new ArgumentException("At least one tag must be provided.", nameof(tags));
            }

            var categoryFilter = string.Join("|", tagList.Select(tag => $"Category={EscapeFilterValue(tag)}"));

            options.Filter = string.IsNullOrEmpty(options.Filter)
                ? categoryFilter
                : $"({options.Filter})&({categoryFilter})";

            return BuildCommand(options);
        }

        public static string BuildExcludeTagsCommand(IEnumerable<string> tags, TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();

            var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                              .Select(t => t.TrimStart('@').Trim())
                              .Where(t => !string.IsNullOrEmpty(t))
                              .ToList();

            if (tagList.Count == 0)
            {
                throw new ArgumentException("At least one tag must be provided.", nameof(tags));
            }

            var excludeFilter = string.Join("&", tagList.Select(tag => $"Category!={EscapeFilterValue(tag)}"));

            options.Filter = string.IsNullOrEmpty(options.Filter)
                ? excludeFilter
                : $"{options.Filter}&{excludeFilter}";

            return BuildCommand(options);
        }

        public static string BuildCustomFilterCommand(string filter, TestRunnerOptions? options = null)
        {
            options ??= new TestRunnerOptions();
            options.Filter = filter;
            return BuildCommand(options);
        }

        private static string BuildCommand(TestRunnerOptions options)
        {
            var command = new StringBuilder();
            command.Append("dotnet test");
            command.Append($" \"{ProjectPath}\"");

            if (!string.IsNullOrEmpty(options.Configuration))
            {
                command.Append($" --configuration {options.Configuration}");
            }

            if (options.EnableParallel && _config.ParallelExecutionEnabled)
            {
                command.Append(" --parallel");
            }

            if (!string.IsNullOrEmpty(options.Filter))
            {
                command.Append($" --filter \"{options.Filter}\"");
            }

            if (options.Verbosity.HasValue)
            {
                var verbosity = options.Verbosity.Value switch
                {
                    TestRunnerVerbosity.Quiet => "quiet",
                    TestRunnerVerbosity.Minimal => "minimal",
                    TestRunnerVerbosity.Normal => "normal",
                    TestRunnerVerbosity.Detailed => "detailed",
                    TestRunnerVerbosity.Diagnostic => "diagnostic",
                    _ => "normal"
                };
                command.Append($" --logger \"console;verbosity={verbosity}\"");
            }

            if (options.GenerateTrx && !string.IsNullOrEmpty(options.TrxFileName))
            {
                command.Append($" --logger \"trx;LogFileName={options.TrxFileName}\"");
            }

            if (options.GenerateNUnitXml && !string.IsNullOrEmpty(options.NUnitXmlFileName))
            {
                command.Append($" --logger \"nunit;LogFileName={options.NUnitXmlFileName}\"");
            }

            if (!string.IsNullOrEmpty(options.AdditionalArguments))
            {
                command.Append($" {options.AdditionalArguments}");
            }

            if (options.NoBuild)
            {
                command.Append(" --no-build");
            }

            if (options.NoRestore)
            {
                command.Append(" --no-restore");
            }

            return command.ToString();
        }

        private static string EscapeFilterValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Replace("\"", "\\\"");
        }

        public static string GetHelpText()
        {
            return @"QualiFrame Reqnroll-BDD Test Runner
=========================================

This runner executes tests via 'dotnet test' (NUnit/VSTest CLI), ensuring full Reqnroll lifecycle.
All hooks in src\QualiFrame.Tests\Hooks are automatically invoked for every execution.

Supported Execution Modes:
--------------------------
1. All Tests
2. Single Scenario/Test (by name pattern)
3. Feature/Suite (by feature name)
4. Tag/Category Filtering (Reqnroll @tags → NUnit Category)
5. Custom Filter Expressions

Example Commands:
-----------------
dotnet test ""src/QualiFrame.Tests/QualiFrame.Tests.csproj""
dotnet test ""src/QualiFrame.Tests/QualiFrame.Tests.csproj"" --filter ""Category=WebDemo""
dotnet test ""src/QualiFrame.Tests/QualiFrame.Tests.csproj"" --parallel";
        }
    }
}

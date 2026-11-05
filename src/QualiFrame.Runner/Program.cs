using System;
using CommandLine;
using QualiFrame.Runner.Core;

namespace QualiFrame.Runner
{
    class Program
    {
        public class Options
        {
            [Option('f', "filter", Required = false, HelpText = "Test filter expression (e.g., Category=WebDemo).")]
            public string? Filter { get; set; }

            [Option('c', "config", Required = false, Default = "Debug", HelpText = "Build configuration (Debug or Release).")]
            public string Configuration { get; set; } = "Debug";

            [Option('p', "parallel", Required = false, HelpText = "Enable parallel execution.")]
            public bool EnableParallel { get; set; }

            [Option('v', "verbosity", Required = false, Default = "Normal", HelpText = "Log verbosity (Quiet, Minimal, Normal, Detailed, Diagnostic).")]
            public string Verbosity { get; set; } = "Normal";
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    (Options opts) => RunTests(opts),
                    errs => 1);
        }

        static int RunTests(Options opts)
        {
            Console.WriteLine("QualiFrame Test Runner CLI");
            Console.WriteLine("==========================");

            if (!Enum.TryParse<TestRunnerVerbosity>(opts.Verbosity, true, out var verbosity))
            {
                verbosity = TestRunnerVerbosity.Normal;
                Console.WriteLine($"Warning: Invalid verbosity '{opts.Verbosity}'. Defaulting to Normal.");
            }

            var runnerOptions = new TestRunnerOptions
            {
                Filter = opts.Filter,
                Configuration = opts.Configuration,
                EnableParallel = opts.EnableParallel,
                Verbosity = verbosity,
                ShowSummary = true,
                BuildBeforeRun = true // Default to building
            };

            try
            {
                var runner = new TestRunner(runnerOptions);
                return runner.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing tests: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return -1;
            }
        }
    }
}

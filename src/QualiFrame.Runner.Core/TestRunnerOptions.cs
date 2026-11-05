namespace QualiFrame.Runner.Core
{
    /// <summary>
    /// Options for configuring test runner execution via dotnet test.
    /// Reuses existing Qualiframe configuration and respects framework settings.
    /// </summary>
    public class TestRunnerOptions
    {
        /// <summary>
        /// Test filter expression (e.g., "Category=WebDemo", "FullyQualifiedName~Login").
        /// Reqnroll tags map to Category attributes (e.g., @WebDemo → Category="WebDemo").
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Specific project path to run tests for.
        /// </summary>
        public string? ProjectPath { get; set; }

        /// <summary>
        /// Build configuration (Debug or Release).
        /// Default: Uses project default configuration.
        /// </summary>
        public string? Configuration { get; set; }

        /// <summary>
        /// Enable parallel execution.
        /// Only effective if ParallelExecutionEnabled is true in RunnerConfiguration.
        /// Respects existing framework parallel execution settings.
        /// </summary>
        public bool EnableParallel { get; set; } = false;

        /// <summary>
        /// Effective parallel execution state.
        /// </summary>
        public bool EffectiveParallelExecution => EnableParallel;

        /// <summary>
        /// Output verbosity level.
        /// </summary>
        public TestRunnerVerbosity? Verbosity { get; set; }

        /// <summary>
        /// Generate TRX (Visual Studio Test Results) file.
        /// </summary>
        public bool GenerateTrx { get; set; } = false;

        /// <summary>
        /// TRX output file name (e.g., "TestResults.trx").
        /// </summary>
        public string TrxFileName { get; set; } = "TestResults.trx";

        /// <summary>
        /// Generate NUnit XML results file.
        /// </summary>
        public bool GenerateNUnitXml { get; set; } = false;

        /// <summary>
        /// NUnit XML output file name (e.g., "TestResults.xml").
        /// </summary>
        public string NUnitXmlFileName { get; set; } = "TestResults.xml";

        /// <summary>
        /// Skip build before test execution.
        /// </summary>
        public bool NoBuild { get; set; } = false;

        /// <summary>
        /// Whether to build before running tests.
        /// </summary>
        public bool BuildBeforeRun { get; set; } = true;

        /// <summary>
        /// Skip restore before test execution.
        /// </summary>
        public bool NoRestore { get; set; } = false;

        /// <summary>
        /// Whether to restore packages.
        /// </summary>
        public bool RestorePackages => !NoRestore;

        /// <summary>
        /// Perform a clean build before execution.
        /// </summary>
        public bool CleanBuild { get; set; } = false;

        /// <summary>
        /// Show execution summary after tests complete.
        /// </summary>
        public bool ShowSummary { get; set; } = true;

        /// <summary>
        /// List of custom loggers to use.
        /// </summary>
        public List<string> Loggers { get; set; } = new List<string>();

        /// <summary>
        /// Whether to collect code coverage.
        /// </summary>
        public bool CollectCoverage { get; set; } = false;

        /// <summary>
        /// Format for code coverage output.
        /// </summary>
        public string CoverageFormat { get; set; } = "opencover";

        /// <summary>
        /// Additional dotnet test arguments (raw string appended to command).
        /// Use for advanced scenarios not covered by other options.
        /// </summary>
        public string? AdditionalArguments { get; set; }
    }

    /// <summary>
    /// Test output verbosity levels.
    /// </summary>
    public enum TestRunnerVerbosity
    {
        Quiet,
        Minimal,
        Normal,
        Detailed,
        Diagnostic
    }
}

using System;

namespace QualiFrame.Framework.Analytics
{
    /// <summary>
    /// Represents a test execution record for ML training and analytics.
    /// Note: This is different from the TestExecutionRecord in TestMetrics.cs which is for general metrics.
    /// </summary>
    public class MLTestExecutionRecord
    {
        /// <summary>
        /// Gets or sets the test name.
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the browser used for execution.
        /// </summary>
        public string Browser { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the environment (QA, Staging, Production).
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the test passed.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Gets or sets the test execution duration.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the execution timestamp.
        /// </summary>
        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata (JSON string).
        /// </summary>
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Test execution data for ML training (internal use).
    /// </summary>
    internal class TestExecutionData
    {
        public string TestName { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public float AverageDuration { get; set; }
        public float SuccessRate { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public DateTime ExecutionTime { get; set; }
        public int ExecutionCount { get; set; }
    }
}

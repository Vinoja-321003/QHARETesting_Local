using System.Collections.Generic;

namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Represents a single keyword test step.
    /// </summary>
    public class KeywordTestStep
    {
        /// <summary>
        /// Gets or sets the step number.
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the keyword name.
        /// </summary>
        public string KeywordName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters for the keyword.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this step is expected to fail.
        /// </summary>
        public bool ExpectedToFail { get; set; }

        /// <summary>
        /// Gets or sets the result of step execution.
        /// </summary>
        public KeywordResult? Result { get; set; }
    }
}

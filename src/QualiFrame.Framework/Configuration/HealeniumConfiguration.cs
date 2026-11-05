namespace QualiFrame.Framework.Configuration
{
    /// <summary>
    /// Configuration for Healenium self-healing test automation.
    /// </summary>
    public class HealeniumConfiguration
    {
        /// <summary>
        /// Gets or sets whether Healenium is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the Healenium backend URL.
        /// </summary>
        public string BackendUrl { get; set; } = "http://localhost:7878";

        /// <summary>
        /// Gets or sets the selector-imitator URL used during healing.
        /// </summary>
        public string ImitatorUrl { get; set; } = "http://localhost:8000";

        /// <summary>
        /// Gets or sets the timeout for healing requests in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to log healing attempts.
        /// </summary>
        public bool LogHealingAttempts { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of healing attempts per element.
        /// </summary>
        public int MaxHealingAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the minimum confidence required to accept a healed locator (0-1).
        /// </summary>
        public double ScoreCap { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets the directory used to persist versioned locator evidence and healing audits.
        /// </summary>
        public string EvidencePath { get; set; } = "Results/HealeniumEvidence";
    }
}

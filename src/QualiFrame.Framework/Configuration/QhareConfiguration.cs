namespace QualiFrame.Framework.Configuration
{
    /// <summary>
    /// Configuration for the QHARE (Qualiframe Adaptive Healing &amp; Reliability Engine) backend.
    /// </summary>
    public class QhareConfiguration
    {
        public bool Enabled { get; set; } = true;

        public string BaseUrl { get; set; } = "http://localhost:8000";

        public string ProjectId { get; set; } = "retail-web";

        /// <summary>
        /// QHARE healing mode: OFF, OBSERVE_ONLY, SUGGEST, REVIEW_REQUIRED, AUTO_HEAL.
        /// </summary>
        public string Mode { get; set; } = "AUTO_HEAL";

        public int TimeoutSeconds { get; set; } = 8;

        /// <summary>
        /// When true, a QHARE outage rethrows the original NoSuchElementException.
        /// </summary>
        public bool FailClosed { get; set; } = true;

        public string FrameworkVersion { get; set; } = "4.43";

        public string? ApiKey { get; set; }

        public bool LogHealingAttempts { get; set; } = true;
    }
}

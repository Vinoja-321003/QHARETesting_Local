using System.Text.Json.Serialization;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    public sealed class QhareHealResponse
    {
        public string Status { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public QhareCandidate? Candidate { get; set; }
        public List<QhareAlternative> Alternatives { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public string Mode { get; set; } = "AUTO_HEAL";
        public List<QhareRecoveryAction> Actions { get; set; } = new();
    }

    public sealed class QhareCandidate
    {
        public QhareLocator? PreferredLocator { get; set; }
        public List<QhareLocator> Locators { get; set; } = new();
        public double Score { get; set; }
        public string NativeSnippet { get; set; } = string.Empty;
    }

    public sealed class QhareLocator
    {
        public string Type { get; set; } = "css";
        public string? Value { get; set; }
        public string? Name { get; set; }
    }

    public sealed class QhareAlternative
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
        public double Score { get; set; }
    }

    public sealed class QhareRecoverResponse
    {
        public string Status { get; set; } = string.Empty;
        public List<QhareRecoveryAction> Actions { get; set; } = new();
    }

    public sealed class QhareRecoveryAction
    {
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("ms")]
        public int Ms { get; set; }

        public QhareLocator? Locator { get; set; }
    }

    public sealed class QhareManifest
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Framework { get; set; } = "selenium";
        public string Mode { get; set; } = "AUTO_HEAL";
        public List<string> LocatorStrategies { get; set; } = new();
        public bool FailClosed { get; set; } = true;
    }

    public sealed class PendingQhareTelemetry
    {
        public string Type { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;
        public string TestId { get; set; } = string.Empty;
        public object Payload { get; set; } = new { };
    }

    /// <summary>
    /// A validated QHARE locator heal captured for reporting (Allure / Extent / logs).
    /// </summary>
    public sealed class QhareHealRecord
    {
        public string Step { get; init; } = string.Empty;
        public string OriginalLocator { get; init; } = string.Empty;
        public string HealedLocator { get; init; } = string.Empty;
        public string Status { get; init; } = "HEALED";
        public double Confidence { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string ValidationPolicy { get; init; } = "ELEMENT_RESOLVED";
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        public string Summary =>
            string.IsNullOrWhiteSpace(HealedLocator)
                ? $"QHARE {Status}: {OriginalLocator}"
                : $"QHARE healed: {OriginalLocator} → {HealedLocator}";
    }
}

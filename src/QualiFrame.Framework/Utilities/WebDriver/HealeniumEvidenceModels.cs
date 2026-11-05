using OpenQA.Selenium;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Versioned locator evidence for one logical element (original test locator + page).
    /// </summary>
    public sealed class LocatorEvidenceRecord
    {
        public string ElementKey { get; set; } = string.Empty;
        public string OriginalLocatorType { get; set; } = string.Empty;
        public string OriginalLocatorValue { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
        public int TrustedVersion { get; set; }
        public List<LocatorBaseline> History { get; set; } = new();
    }

    /// <summary>
    /// One trusted (or superseded) baseline snapshot for a logical element.
    /// </summary>
    public sealed class LocatorBaseline
    {
        public int Version { get; set; }
        public DateTimeOffset CapturedAt { get; set; }
        public string Source { get; set; } = "success";
        public string LocatorType { get; set; } = string.Empty;
        public string LocatorValue { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public HealeniumNode? TargetNode { get; set; }
        public List<HealeniumNode> NodePath { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);
        public string? Feature { get; set; }
        public string? Scenario { get; set; }
        public string? Step { get; set; }
        public string? ClassName { get; set; }
        public string? MethodName { get; set; }
        public double? HealingConfidence { get; set; }
        public string? PreviousLocatorType { get; set; }
        public string? PreviousLocatorValue { get; set; }
        public bool Trusted { get; set; } = true;
    }

    public sealed class HealingAttemptRecord
    {
        public DateTimeOffset Timestamp { get; set; }
        public string OriginalLocatorType { get; set; } = string.Empty;
        public string OriginalLocatorValue { get; set; } = string.Empty;
        public string? HealedLocatorType { get; set; }
        public string? HealedLocatorValue { get; set; }
        public string Url { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double ScoreCap { get; set; }
        public bool Healed { get; set; }
        public bool StepSucceeded { get; set; }
        public bool Promoted { get; set; }
        public string? Reason { get; set; }
        public int? HistoricalVersionUsed { get; set; }
        public List<HealingCandidateScore> Candidates { get; set; } = new();
        public HealeniumNode? HistoricalNode { get; set; }
        public HealeniumNode? CurrentNode { get; set; }
        public string? Feature { get; set; }
        public string? Scenario { get; set; }
        public string? Step { get; set; }
    }

    public sealed class HealingCandidateScore
    {
        public string LocatorType { get; set; } = string.Empty;
        public string LocatorValue { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? MatchedFeatures { get; set; }
        public bool Selected { get; set; }
    }

    public sealed class PendingLocatorCapture
    {
        public string LocatorType { get; set; } = string.Empty;
        public string LocatorValue { get; set; } = string.Empty;
        public string? StoredLocatorType { get; set; }
        public string? StoredLocatorValue { get; set; }
        public string? PreviousLocatorType { get; set; }
        public string? PreviousLocatorValue { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Command { get; set; } = "findElement";
        public List<HealeniumNode> NodePath { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);
        public string? ClassName { get; set; }
        public string? MethodName { get; set; }
        public List<string> ElementIds { get; set; } = new();
    }

    public sealed class PendingHealingCapture
    {
        public HealingAttemptRecord Attempt { get; set; } = new();
        public PendingLocatorCapture Snapshot { get; set; } = new();
    }

    public sealed class ScoredHealingCandidate
    {
        public By Locator { get; set; } = By.CssSelector("*");
        public double Score { get; set; }
        public string MatchedFeatures { get; set; } = string.Empty;
        public List<HealeniumNode> NodePath { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);
        public int HistoricalVersion { get; set; }
        public HealeniumNode? HistoricalNode { get; set; }
        public OpenQA.Selenium.IWebElement? Element { get; set; }
    }
}

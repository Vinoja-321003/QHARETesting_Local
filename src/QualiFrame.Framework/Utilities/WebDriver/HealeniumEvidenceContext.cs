namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Per-scenario evidence scope. Bound by Reqnroll hooks so baselines promote only after a passing step.
    /// </summary>
    public sealed class HealeniumEvidenceScope
    {
        public string Feature { get; init; } = string.Empty;
        public string Scenario { get; init; } = string.Empty;
        public string? Step { get; set; }
        public List<PendingLocatorCapture> PendingCaptures { get; } = new();
        public List<PendingHealingCapture> PendingHealings { get; } = new();

        public void BeginStep(string stepText)
        {
            Step = stepText;
            PendingCaptures.Clear();
            PendingHealings.Clear();
        }

        public void QueueSuccess(PendingLocatorCapture capture)
        {
            var key = CaptureKey(capture);
            var index = PendingCaptures.FindIndex(existing => CaptureKey(existing) == key);
            if (index >= 0)
            {
                PendingCaptures[index] = capture;
            }
            else
            {
                PendingCaptures.Add(capture);
            }
        }

        public void QueueHealing(PendingHealingCapture healing)
        {
            var key = CaptureKey(healing.Snapshot);
            var index = PendingHealings.FindIndex(existing => CaptureKey(existing.Snapshot) == key);
            if (index >= 0)
            {
                PendingHealings[index] = healing;
            }
            else
            {
                PendingHealings.Add(healing);
            }
        }

        private static string CaptureKey(PendingLocatorCapture capture) =>
            $"{capture.LocatorType}|{capture.LocatorValue}|{HealeniumEvidenceStore.NormalizeUrl(capture.Url)}";
    }

    /// <summary>
    /// Async-local binder so parallel scenarios do not share pending evidence.
    /// </summary>
    public static class HealeniumEvidenceContext
    {
        private static readonly AsyncLocal<HealeniumEvidenceScope?> CurrentScope = new();

        public static bool IsBound => CurrentScope.Value != null;

        public static HealeniumEvidenceScope? Current => CurrentScope.Value;

        public static void Bind(HealeniumEvidenceScope scope)
        {
            CurrentScope.Value = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public static void Unbind()
        {
            CurrentScope.Value = null;
        }
    }
}

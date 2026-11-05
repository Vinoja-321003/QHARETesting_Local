using OpenQA.Selenium;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Per-scenario QHARE scope. Validated heals flush after a passing step; failed/rejected heals flush even when the step fails.
    /// </summary>
    public sealed class QhareHealingScope
    {
        public string ExecutionId { get; init; } = Guid.NewGuid().ToString("N");
        public string TestId { get; init; } = string.Empty;
        public string Feature { get; init; } = string.Empty;
        public string? Step { get; set; }
        public QhareClient? Client { get; set; }
        public Dictionary<string, By> HealedLocators { get; } = new(StringComparer.Ordinal);
        public HashSet<string> FailedLocators { get; } = new(StringComparer.Ordinal);
        public List<PendingQhareTelemetry> PendingTelemetry { get; } = new();

        /// <summary>Validated heals for the current Reqnroll step (cleared on <see cref="BeginStep"/>).</summary>
        public List<QhareHealRecord> StepHeals { get; } = new();

        /// <summary>Validated heals promoted after each step for scenario-level reporting.</summary>
        public List<QhareHealRecord> ScenarioHeals { get; } = new();

        public void BeginStep(string stepText)
        {
            Step = stepText;
            HealedLocators.Clear();
            FailedLocators.Clear();
            PendingTelemetry.Clear();
            StepHeals.Clear();
        }

        public void QueueTelemetry(PendingQhareTelemetry eventPayload)
        {
            PendingTelemetry.Add(eventPayload);
        }

        public void RecordValidatedHeal(QhareHealRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            StepHeals.Add(record);
        }
    }

    public static class QhareHealingContext
    {
        private static readonly AsyncLocal<QhareHealingScope?> CurrentScope = new();

        public static QhareHealingScope? Current => CurrentScope.Value;

        public static void Bind(QhareHealingScope scope)
        {
            CurrentScope.Value = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public static void Unbind()
        {
            CurrentScope.Value = null;
        }

        public static void AttachClient(QhareClient client)
        {
            if (CurrentScope.Value != null)
            {
                CurrentScope.Value.Client = client;
            }
        }

        /// <summary>
        /// Returns validated heals for the current step for reporting (pass or fail).
        /// Promotes heals to scenario history when the step passed; always clears step-level records.
        /// </summary>
        public static IReadOnlyList<QhareHealRecord> ConsumeStepHealsForReporting(bool stepPassed)
        {
            var scope = CurrentScope.Value;
            if (scope == null || scope.StepHeals.Count == 0)
            {
                return Array.Empty<QhareHealRecord>();
            }

            var heals = scope.StepHeals.ToList();
            scope.StepHeals.Clear();

            // Promote for scenario-level reporting whether the step passed or failed.
            scope.ScenarioHeals.AddRange(heals);
            return heals;
        }

        public static void CommitBoundStep(bool stepPassed)
        {
            var scope = CurrentScope.Value;
            if (scope?.Client == null)
            {
                return;
            }

            foreach (var pending in scope.PendingTelemetry)
            {
                if (stepPassed || IsFailureTelemetry(pending.Type))
                {
                    scope.Client.Emit(pending.Type, pending.ExecutionId, pending.TestId, pending.Payload);
                }
            }

            scope.PendingTelemetry.Clear();
            scope.HealedLocators.Clear();
            scope.FailedLocators.Clear();
        }

        private static bool IsFailureTelemetry(string? type) =>
            string.Equals(type, "heal.failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "heal.rejected", StringComparison.OrdinalIgnoreCase);
    }
}

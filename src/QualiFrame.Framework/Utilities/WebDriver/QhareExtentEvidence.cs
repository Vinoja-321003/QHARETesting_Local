using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Publishes validated QHARE heals into the active Extent step/scenario so Spark reports
    /// show that self-healing was applied (mirrors <see cref="QhareAllureEvidence"/>).
    /// </summary>
    public static class QhareExtentEvidence
    {
        public const string HealedMarker = "[QHARE healed]";
        public const string PassedWithHealingMessage = "Passed (QHARE healed)";
        public const string HealedOnFailedStepMessage = "QHARE healed locator(s) before this step failed";

        /// <summary>
        /// Marks the current Extent step as healed and logs locator remap details.
        /// Works for both passed and failed steps that had validated QHARE heals.
        /// </summary>
        public static void PublishStepHeals(IReadOnlyList<QhareHealRecord> heals, bool stepPassed)
        {
            if (heals == null || heals.Count == 0)
            {
                return;
            }

            try
            {
                var step = ExtentReportManager.GetCurrentStep();
                if (step == null)
                {
                    Logger.Debug("QHARE Extent evidence skipped: no current step node.");
                    return;
                }

                AppendHealedMarker(step);
                try
                {
                    step.AssignCategory("qhare-healed", "self-healing");
                }
                catch (Exception categoryEx)
                {
                    Logger.Debug("Failed to assign QHARE categories on Extent step: {Error}", categoryEx.Message);
                }

                if (stepPassed)
                {
                    step.Pass(PassedWithHealingMessage);
                }
                else
                {
                    step.Warning(HealedOnFailedStepMessage);
                }

                step.Info($"QHARE self-healing applied ({heals.Count} locator(s)).");
                PublishHealNodes(step, heals, stepPassed);
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to publish QHARE heals to Extent step: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Backward-compatible alias for passing steps.
        /// </summary>
        public static void PublishPassingStepHeals(IReadOnlyList<QhareHealRecord> heals)
            => PublishStepHeals(heals, stepPassed: true);

        /// <summary>
        /// Tags the current Extent scenario when any step used QHARE self-healing.
        /// </summary>
        public static void AnnotateScenario(IReadOnlyList<QhareHealRecord> scenarioHeals)
        {
            if (scenarioHeals == null || scenarioHeals.Count == 0)
            {
                return;
            }

            try
            {
                var scenario = ExtentReportManager.GetCurrentScenario();
                if (scenario == null)
                {
                    Logger.Debug("QHARE Extent scenario annotation skipped: no current scenario node.");
                    return;
                }

                AppendHealedMarker(scenario);
                try
                {
                    scenario.AssignCategory("qhare-healed", "self-healing");
                }
                catch (Exception categoryEx)
                {
                    Logger.Debug("Failed to assign QHARE categories on Extent scenario: {Error}", categoryEx.Message);
                }

                var summary = string.Join(
                    Environment.NewLine,
                    scenarioHeals.Select(h =>
                        $"• {h.OriginalLocator} → {h.HealedLocator} (confidence {h.Confidence:0.####}, {h.Status})"));

                scenario.Info(
                    $"QHARE self-healing applied ({scenarioHeals.Count} locator(s)):{Environment.NewLine}{summary}");
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to annotate QHARE heals on Extent scenario: {Error}", ex.Message);
            }
        }

        private static void PublishHealNodes(
            AventStack.ExtentReports.ExtentTest step,
            IReadOnlyList<QhareHealRecord> heals,
            bool stepPassed)
        {
            foreach (var heal in heals)
            {
                var nodeName = Truncate(heal.Summary, 180);
                var healNode = step.CreateNode(nodeName);
                if (stepPassed)
                {
                    healNode.Pass(PassedWithHealingMessage);
                }
                else
                {
                    healNode.Warning(HealedOnFailedStepMessage);
                }

                healNode.Info($"Original: {heal.OriginalLocator}");
                healNode.Info($"Healed: {heal.HealedLocator}");
                healNode.Info($"Status: {heal.Status} | Confidence: {heal.Confidence:0.####}");
                if (!string.IsNullOrWhiteSpace(heal.Reason))
                {
                    healNode.Info($"Reason: {heal.Reason}");
                }

                if (!string.IsNullOrWhiteSpace(heal.ValidationPolicy))
                {
                    healNode.Info($"Validation: {heal.ValidationPolicy}");
                }
            }
        }

        private static void AppendHealedMarker(AventStack.ExtentReports.ExtentTest test)
        {
            var model = test.Model;
            if (model == null)
            {
                return;
            }

            var name = model.Name;
            if (string.IsNullOrWhiteSpace(name) ||
                name.Contains(HealedMarker, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            model.Name = $"{name} {HealedMarker}";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..(maxLength - 1)] + "…";
        }
    }
}

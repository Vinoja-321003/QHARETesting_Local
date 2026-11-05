using System.Text;
using System.Text.Json;
using Allure.Net.Commons;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Publishes validated QHARE heals into the active Allure test/step so reports show self-healing.
    /// </summary>
    public static class QhareAllureEvidence
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Annotates the current Allure step with nested heal details when the step passed via QHARE.
        /// </summary>
        public static void PublishPassingStepHeals(IReadOnlyList<QhareHealRecord> heals)
        {
            if (heals == null || heals.Count == 0)
            {
                return;
            }

            try
            {
                AllureLifecycle.Instance.UpdateStep(step =>
                {
                    if (step == null)
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(step.name) ||
                        !step.name.Contains("[QHARE healed]", StringComparison.OrdinalIgnoreCase))
                    {
                        step.name = $"{step.name} [QHARE healed]";
                    }

                    step.parameters ??= new List<Parameter>();
                    EnsureParameter(step.parameters, "qhare.healed", "true");
                    EnsureParameter(step.parameters, "qhare.healCount", heals.Count.ToString());
                });

                foreach (var heal in heals)
                {
                    var nestedName = Truncate(heal.Summary, 180);
                    AllureLifecycle.Instance.StartStep(new StepResult
                    {
                        name = nestedName,
                        start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });

                    AllureLifecycle.Instance.UpdateStep(step =>
                    {
                        step.status = Status.passed;
                        step.statusDetails = new StatusDetails
                        {
                            message = string.IsNullOrWhiteSpace(heal.Reason)
                                ? "QHARE self-healing applied."
                                : heal.Reason
                        };
                        step.parameters = new List<Parameter>
                        {
                            new() { name = "originalLocator", value = heal.OriginalLocator },
                            new() { name = "healedLocator", value = heal.HealedLocator },
                            new() { name = "status", value = heal.Status },
                            new() { name = "confidence", value = heal.Confidence.ToString("0.####") },
                            new() { name = "validationPolicy", value = heal.ValidationPolicy }
                        };
                    });

                    AllureLifecycle.Instance.StopStep();
                }

                var payload = JsonSerializer.Serialize(heals.Select(h => new
                {
                    h.Step,
                    h.OriginalLocator,
                    h.HealedLocator,
                    h.Status,
                    h.Confidence,
                    h.Reason,
                    h.ValidationPolicy,
                    timestamp = h.Timestamp.ToString("O")
                }), JsonOptions);

                AllureApi.AddAttachment(
                    "QHARE Self-Healing Details",
                    "application/json",
                    Encoding.UTF8.GetBytes(payload),
                    ".json");
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to publish QHARE heals to Allure step: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Tags the Allure test case when any passing step used QHARE self-healing.
        /// </summary>
        public static void AnnotateTestCase(TestResult testCase, IReadOnlyList<QhareHealRecord> scenarioHeals)
        {
            if (testCase == null || scenarioHeals == null || scenarioHeals.Count == 0)
            {
                return;
            }

            testCase.labels ??= new List<Label>();
            EnsureLabel(testCase.labels, "tag", "qhare-healed");
            EnsureLabel(testCase.labels, "tag", "self-healing");
            EnsureLabel(testCase.labels, "qhare", "healed");

            var summary = string.Join(
                Environment.NewLine,
                scenarioHeals.Select(h =>
                    $"• {h.OriginalLocator} → {h.HealedLocator} (confidence {h.Confidence:0.####}, {h.Status})"));

            var note =
                $"QHARE self-healing applied ({scenarioHeals.Count} locator(s)):{Environment.NewLine}{summary}";

            testCase.description = string.IsNullOrWhiteSpace(testCase.description)
                ? note
                : $"{testCase.description}{Environment.NewLine}{Environment.NewLine}{note}";

            testCase.parameters ??= new List<Parameter>();
            EnsureParameter(testCase.parameters, "qhare.healed", "true");
            EnsureParameter(testCase.parameters, "qhare.healCount", scenarioHeals.Count.ToString());
        }

        private static void EnsureLabel(IList<Label> labels, string name, string value)
        {
            if (labels.Any(l =>
                    string.Equals(l.name, name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(l.value, value, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            labels.Add(new Label { name = name, value = value });
        }

        private static void EnsureParameter(IList<Parameter> parameters, string name, string value)
        {
            var existing = parameters.FirstOrDefault(p =>
                string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.value = value;
                return;
            }

            parameters.Add(new Parameter { name = name, value = value });
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

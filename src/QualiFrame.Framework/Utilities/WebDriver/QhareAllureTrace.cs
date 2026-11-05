using System.Globalization;
using System.Text;
using Allure.Net.Commons;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Writes one QHARE healing transaction as a nested Allure step and a text attachment.
    /// </summary>
    internal static class QhareAllureTrace
    {
        public static void Record(QhareHealingTrace trace)
        {
            if (!IsAllureEnabled())
            {
                return;
            }

            try
            {
                var lifecycle = AllureLifecycle.Instance;
                if (!lifecycle.Context.HasTest && !lifecycle.Context.HasFixture)
                {
                    return;
                }

                var started = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                lifecycle.StartStep(new StepResult
                {
                    name = trace.StepTitle(),
                    start = started
                });
                Attachment? attachment = null;
                try
                {
                    AllureApi.AddAttachment(
                        "QHARE healing transaction",
                        "text/plain",
                        Encoding.UTF8.GetBytes(trace.ToText()),
                        ".txt");

                    lifecycle.UpdateStep(step =>
                    {
                        step.status = trace.AllureStatus();
                        step.stage = Stage.finished;
                        step.stop = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        if (step.status != Status.passed && !string.IsNullOrWhiteSpace(trace.Reason))
                        {
                            step.statusDetails = new StatusDetails { message = trace.Reason };
                        }

                        attachment = step.attachments?.LastOrDefault();
                    });
                }
                finally
                {
                    lifecycle.StopStep();
                }

                PublishOnTest(lifecycle, trace, attachment, started);
            }
            catch (Exception ex)
            {
                Logger.Debug("QHARE Allure trace skipped: {Message}", ex.Message);
            }
        }

        private static void PublishOnTest(AllureLifecycle lifecycle, QhareHealingTrace trace, Attachment? attachment, long started)
        {
            if (!lifecycle.Context.HasTest)
            {
                return;
            }

            var text = trace.ToText();
            var html = "<pre>" + System.Net.WebUtility.HtmlEncode(text) + "</pre>";
            lifecycle.UpdateTestCase(test =>
            {
                if (!test.labels.Any(label => label.name == "tag" && label.value == "QHARE"))
                {
                    test.labels.Add(new Label { name = "tag", value = "QHARE" });
                }

                test.description = string.IsNullOrWhiteSpace(test.description) ? text : test.description + "\n\n" + text;
                test.descriptionHtml = string.IsNullOrWhiteSpace(test.descriptionHtml) ? html : test.descriptionHtml + html;

                var visible = new StepResult
                {
                    name = trace.StepTitle(),
                    status = trace.AllureStatus(),
                    stage = Stage.finished,
                    start = started,
                    stop = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                if (visible.status != Status.passed && !string.IsNullOrWhiteSpace(trace.Reason))
                {
                    visible.statusDetails = new StatusDetails { message = trace.Reason };
                }

                if (attachment != null)
                {
                    visible.attachments.Add(new Attachment
                    {
                        name = attachment.name,
                        source = attachment.source,
                        type = attachment.type
                    });
                }

                var insertAt = test.steps.TakeWhile(step => step.name != null && step.name.StartsWith("QHARE ", StringComparison.Ordinal)).Count();
                test.steps.Insert(insertAt, visible);
            });
        }

        private static bool IsAllureEnabled()
        {
            try
            {
                return TestConfiguration.Instance.AllureReportEnabled;
            }
            catch (Exception ex)
            {
                Logger.Debug("QHARE Allure trace skipped: {Message}", ex.Message);
                return false;
            }
        }
    }

    internal sealed class QhareHealingTrace
    {
        public string Outcome { get; init; } = "UNRESOLVED";
        public string OriginalLocator { get; init; } = string.Empty;
        public string? HealedLocator { get; init; }
        public double? Confidence { get; init; }
        public string? Reason { get; init; }
        public string? Command { get; init; }
        public string? Step { get; init; }
        public string? TestId { get; init; }
        public string? ExecutionId { get; init; }
        public string? Mode { get; init; }

        public string StepTitle()
        {
            var arrow = string.IsNullOrWhiteSpace(HealedLocator) ? string.Empty : $" -> {HealedLocator}";
            var confidence = Confidence is null
                ? string.Empty
                : $" ({Confidence.Value.ToString("0.###", CultureInfo.InvariantCulture)})";
            return $"QHARE {Outcome}: {OriginalLocator}{arrow}{confidence}";
        }

        public Status AllureStatus() =>
            Outcome.Equals("HEALED", StringComparison.OrdinalIgnoreCase) ||
            Outcome.Equals("RECOVERED", StringComparison.OrdinalIgnoreCase)
                ? Status.passed
                : Outcome.Equals("FAILED", StringComparison.OrdinalIgnoreCase)
                    ? Status.broken
                    : Status.failed;

        public string ToText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("QHARE healing transaction");
            builder.AppendLine($"outcome: {Outcome}");
            builder.AppendLine($"originalLocator: {OriginalLocator}");
            if (!string.IsNullOrWhiteSpace(HealedLocator))
            {
                builder.AppendLine($"healedLocator: {HealedLocator}");
            }

            if (Confidence is not null)
            {
                builder.AppendLine($"confidence: {Confidence.Value.ToString("0.####", CultureInfo.InvariantCulture)}");
            }

            if (!string.IsNullOrWhiteSpace(Reason))
            {
                builder.AppendLine($"reason: {Reason}");
            }

            if (!string.IsNullOrWhiteSpace(Command))
            {
                builder.AppendLine($"command: {Command}");
            }

            if (!string.IsNullOrWhiteSpace(Step))
            {
                builder.AppendLine($"step: {Step}");
            }

            if (!string.IsNullOrWhiteSpace(TestId))
            {
                builder.AppendLine($"test: {TestId}");
            }

            if (!string.IsNullOrWhiteSpace(ExecutionId))
            {
                builder.AppendLine($"executionId: {ExecutionId}");
            }

            if (!string.IsNullOrWhiteSpace(Mode))
            {
                builder.AppendLine($"mode: {Mode}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}

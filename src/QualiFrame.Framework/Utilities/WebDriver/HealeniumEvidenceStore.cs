using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// File-backed versioned locator evidence. Trusted baselines are appended, never blindly overwritten.
    /// Records are keyed by the original test locator plus normalized page URL.
    /// </summary>
    public sealed class HealeniumEvidenceStore
    {
        private static readonly ConcurrentDictionary<string, HealeniumEvidenceStore> Stores = new(StringComparer.OrdinalIgnoreCase);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _root;
        private readonly string _baselineDir;
        private readonly string _auditDir;
        private readonly object _gate = new();

        public HealeniumEvidenceStore(string rootPath)
        {
            _root = Path.GetFullPath(string.IsNullOrWhiteSpace(rootPath) ? "Results/HealeniumEvidence" : rootPath);
            _baselineDir = Path.Combine(_root, "baselines");
            _auditDir = Path.Combine(_root, "audits");
            Directory.CreateDirectory(_baselineDir);
            Directory.CreateDirectory(_auditDir);
        }

        public string RootPath => _root;

        public static HealeniumEvidenceStore ForConfiguration(HealeniumConfiguration config)
        {
            var path = string.IsNullOrWhiteSpace(config.EvidencePath)
                ? "Results/HealeniumEvidence"
                : config.EvidencePath;
            var full = Path.GetFullPath(path);
            return Stores.GetOrAdd(full, p => new HealeniumEvidenceStore(p));
        }

        public static string NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url.Trim().TrimEnd('/');
            }

            return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}".TrimEnd('/');
        }

        public static string ElementKey(string locatorType, string locatorValue, string url)
        {
            var raw = $"{NormalizeType(locatorType)}|{locatorValue}|{NormalizeUrl(url)}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }

        public LocatorEvidenceRecord? Get(string locatorType, string locatorValue, string url)
        {
            var path = BaselinePath(locatorType, locatorValue, url);
            lock (_gate)
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<LocatorEvidenceRecord>(json, JsonOptions);
            }
        }

        public IReadOnlyList<LocatorBaseline> GetBaselinesNewestFirst(string locatorType, string locatorValue, string url)
        {
            var record = Get(locatorType, locatorValue, url);
            if (record == null || record.History.Count == 0)
            {
                return Array.Empty<LocatorBaseline>();
            }

            var trusted = record.History
                .Where(b => b.Trusted)
                .OrderByDescending(b => b.Version)
                .ToList();
            var rest = record.History
                .Where(b => !b.Trusted)
                .OrderByDescending(b => b.Version)
                .ToList();
            return trusted.Concat(rest).ToList();
        }

        public IReadOnlyList<LocatorBaseline> FindRelatedBaselines(
            string locatorType,
            string locatorValue,
            string url,
            double minSimilarity = HealeniumLocatorMapper.RelatedLocatorMinSimilarity)
        {
            var page = NormalizeUrl(url);
            var related = new List<(double Similarity, LocatorBaseline Baseline)>();

            lock (_gate)
            {
                if (!Directory.Exists(_baselineDir))
                {
                    return Array.Empty<LocatorBaseline>();
                }

                foreach (var file in Directory.GetFiles(_baselineDir, "*.json"))
                {
                    LocatorEvidenceRecord? record;
                    try
                    {
                        record = JsonSerializer.Deserialize<LocatorEvidenceRecord>(File.ReadAllText(file), JsonOptions);
                    }
                    catch
                    {
                        continue;
                    }

                    if (record == null ||
                        !string.Equals(NormalizeUrl(record.PageUrl), page, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.Equals(record.OriginalLocatorValue, locatorValue, StringComparison.Ordinal) &&
                        string.Equals(NormalizeType(record.OriginalLocatorType), NormalizeType(locatorType), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var identityScore = HealeniumLocatorMapper.LocatorSimilarity(
                        locatorType, locatorValue, record.OriginalLocatorType, record.OriginalLocatorValue);

                    foreach (var baseline in record.History.Where(item => item.Trusted))
                    {
                        var storedScore = HealeniumLocatorMapper.LocatorSimilarity(
                            locatorType, locatorValue, baseline.LocatorType, baseline.LocatorValue);
                        var similarity = Math.Max(identityScore, storedScore);
                        if (similarity >= minSimilarity)
                        {
                            related.Add((similarity, baseline));
                        }
                    }
                }
            }

            return related
                .OrderByDescending(item => item.Similarity)
                .ThenByDescending(item => item.Baseline.Version)
                .Select(item => item.Baseline)
                .ToList();
        }

        public LocatorBaseline? GetTrustedBaseline(string locatorType, string locatorValue, string url)
        {
            var record = Get(locatorType, locatorValue, url);
            return record?.History
                .Where(b => b.Trusted && b.Version == record.TrustedVersion)
                .OrderByDescending(b => b.Version)
                .FirstOrDefault()
                ?? record?.History.Where(b => b.Trusted).OrderByDescending(b => b.Version).FirstOrDefault();
        }

        public LocatorEvidenceRecord CommitSuccess(PendingLocatorCapture capture, HealeniumEvidenceScope? scope)
        {
            return UpsertBaseline(capture, scope, source: "success", confidence: null);
        }

        public LocatorEvidenceRecord PromoteHealing(PendingHealingCapture healing, HealeniumEvidenceScope? scope)
        {
            healing.Snapshot.StoredLocatorType = healing.Attempt.HealedLocatorType ?? healing.Snapshot.LocatorType;
            healing.Snapshot.StoredLocatorValue = healing.Attempt.HealedLocatorValue ?? healing.Snapshot.LocatorValue;
            healing.Snapshot.PreviousLocatorType = healing.Attempt.OriginalLocatorType;
            healing.Snapshot.PreviousLocatorValue = healing.Attempt.OriginalLocatorValue;

            var record = UpsertBaseline(healing.Snapshot, scope, source: "healed", confidence: healing.Attempt.Confidence);

            healing.Attempt.StepSucceeded = true;
            healing.Attempt.Promoted = true;
            healing.Attempt.Reason = "Healed locator promoted after successful step.";
            AppendAudit(healing.Attempt);
            return record;
        }

        public void RejectHealing(PendingHealingCapture healing, string reason)
        {
            healing.Attempt.StepSucceeded = false;
            healing.Attempt.Promoted = false;
            healing.Attempt.Reason = reason;
            AppendAudit(healing.Attempt);
        }

        public void RecordFailedAttempt(HealingAttemptRecord attempt)
        {
            attempt.Healed = false;
            attempt.Promoted = false;
            attempt.Reason ??= "No candidate exceeded the confidence threshold.";
            AppendAudit(attempt);
        }

        public void CommitBoundStep(bool stepSucceeded)
        {
            var scope = HealeniumEvidenceContext.Current;
            if (scope == null)
            {
                return;
            }

            if (stepSucceeded)
            {
                foreach (var capture in scope.PendingCaptures.ToList())
                {
                    CommitSuccess(capture, scope);
                }

                foreach (var healing in scope.PendingHealings.ToList())
                {
                    PromoteHealing(healing, scope);
                }
            }
            else
            {
                foreach (var healing in scope.PendingHealings.ToList())
                {
                    RejectHealing(healing, "Step failed after healing; baseline was not promoted.");
                }
            }

            scope.PendingCaptures.Clear();
            scope.PendingHealings.Clear();
        }

        private LocatorEvidenceRecord UpsertBaseline(
            PendingLocatorCapture capture,
            HealeniumEvidenceScope? scope,
            string source,
            double? confidence)
        {
            var identityType = capture.LocatorType;
            var identityValue = capture.LocatorValue;
            var storedType = string.IsNullOrWhiteSpace(capture.StoredLocatorType) ? identityType : capture.StoredLocatorType;
            var storedValue = string.IsNullOrWhiteSpace(capture.StoredLocatorValue) ? identityValue : capture.StoredLocatorValue;
            var url = NormalizeUrl(capture.Url);
            var path = BaselinePath(identityType, identityValue, capture.Url);
            var target = capture.NodePath.LastOrDefault();

            lock (_gate)
            {
                LocatorEvidenceRecord record;
                if (File.Exists(path))
                {
                    record = JsonSerializer.Deserialize<LocatorEvidenceRecord>(File.ReadAllText(path), JsonOptions)
                             ?? NewRecord(identityType, identityValue, url);
                }
                else
                {
                    record = NewRecord(identityType, identityValue, url);
                }

                var latest = record.History.OrderByDescending(b => b.Version).FirstOrDefault();
                if (latest != null &&
                    source != "healed" &&
                    string.Equals(latest.LocatorType, storedType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(latest.LocatorValue, storedValue, StringComparison.Ordinal) &&
                    HealeniumEvidenceScorer.IsSameFingerprint(latest.TargetNode, target))
                {
                    latest.CapturedAt = DateTimeOffset.UtcNow;
                    latest.Feature = scope?.Feature ?? latest.Feature;
                    latest.Scenario = scope?.Scenario ?? latest.Scenario;
                    latest.Step = scope?.Step ?? latest.Step;
                    WriteRecord(path, record);
                    return record;
                }

                var version = (latest?.Version ?? 0) + 1;
                var baseline = new LocatorBaseline
                {
                    Version = version,
                    CapturedAt = DateTimeOffset.UtcNow,
                    Source = source,
                    LocatorType = storedType,
                    LocatorValue = storedValue,
                    Url = capture.Url,
                    TargetNode = target,
                    NodePath = capture.NodePath,
                    Attributes = capture.Attributes,
                    Feature = scope?.Feature,
                    Scenario = scope?.Scenario,
                    Step = scope?.Step,
                    ClassName = capture.ClassName,
                    MethodName = capture.MethodName,
                    HealingConfidence = confidence,
                    PreviousLocatorType = capture.PreviousLocatorType,
                    PreviousLocatorValue = capture.PreviousLocatorValue,
                    Trusted = true
                };

                record.History.Add(baseline);
                record.TrustedVersion = version;
                WriteRecord(path, record);

                Logger.Information(
                    "Healenium evidence baseline v{Version} stored for {Type}({Locator}) on {Url} ({Source})",
                    version, identityType, identityValue, url, source);

                return record;
            }
        }

        private void WriteRecord(string path, LocatorEvidenceRecord record)
        {
            var json = JsonSerializer.Serialize(record, JsonOptions);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, json);
        }

        private void AppendAudit(HealingAttemptRecord attempt)
        {
            try
            {
                var file = Path.Combine(_auditDir, $"healing-{DateTime.UtcNow:yyyy-MM-dd}.jsonl");
                var line = JsonSerializer.Serialize(attempt, JsonOptions);
                lock (_gate)
                {
                    File.AppendAllText(file, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium evidence audit write skipped: {Message}", ex.Message);
            }
        }

        private string BaselinePath(string locatorType, string locatorValue, string url)
        {
            var key = ElementKey(locatorType, locatorValue, url);
            return Path.Combine(_baselineDir, $"{key}.json");
        }

        private static LocatorEvidenceRecord NewRecord(string type, string value, string url) =>
            new()
            {
                ElementKey = ElementKey(type, value, url),
                OriginalLocatorType = type,
                OriginalLocatorValue = value,
                PageUrl = url
            };

        private static string NormalizeType(string type) =>
            (type ?? string.Empty).Replace("By.", "", StringComparison.OrdinalIgnoreCase).Trim();
    }
}

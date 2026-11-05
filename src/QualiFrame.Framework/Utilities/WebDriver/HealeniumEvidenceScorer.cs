namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Scores a current DOM node against historical locator evidence.
    /// </summary>
    public static class HealeniumEvidenceScorer
    {
        public const double DefaultScoreCap = 0.5;
        public const double UnexpectedElementThreshold = 0.35;
        public const double SameFingerprintThreshold = 0.95;
        public static bool IsSameFingerprint(HealeniumNode? left, HealeniumNode? right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (IdsEqual(left.Id, right.Id) && TagsEqual(left.Tag, right.Tag))
            {
                return true;
            }

            return Score(left, right) >= SameFingerprintThreshold;
        }

        public static double Score(
            HealeniumNode? historical,
            HealeniumNode? current,
            IReadOnlyList<HealeniumNode>? historicalPath = null,
            IReadOnlyList<HealeniumNode>? currentPath = null)
        {
            if (historical == null || current == null)
            {
                return 0;
            }

            double score = 0;
            double weight = 0;

            Add(ref score, ref weight, 0.20, TagsEqual(historical.Tag, current.Tag));
            Add(ref score, ref weight, 0.24, IdsEqual(historical.Id, current.Id) ? 1 : 0);
            Add(ref score, ref weight, 0.14, ClassOverlap(historical.Classes, current.Classes));
            Add(ref score, ref weight, 0.18, AttributeOverlap(historical.Other, current.Other));
            Add(ref score, ref weight, 0.16, TextSimilarity(historical.InnerText, current.InnerText));
            Add(ref score, ref weight, 0.08, AncestorOverlap(historicalPath, currentPath));

            return weight <= 0 ? 0 : Math.Round(score / weight, 4);
        }

        public static string DescribeMatches(
            HealeniumNode? historical,
            HealeniumNode? current,
            IReadOnlyList<HealeniumNode>? historicalPath = null,
            IReadOnlyList<HealeniumNode>? currentPath = null)
        {
            if (historical == null || current == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (TagsEqual(historical.Tag, current.Tag))
            {
                parts.Add($"tag={current.Tag}");
            }

            if (IdsEqual(historical.Id, current.Id) && !string.IsNullOrWhiteSpace(current.Id))
            {
                parts.Add($"id={current.Id}");
            }

            var classes = (historical.Classes ?? new List<string>())
                .Intersect(current.Classes ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (classes.Count > 0)
            {
                parts.Add("classes=" + string.Join(".", classes));
            }

            foreach (var pair in SharedAttributes(historical.Other, current.Other).Take(4))
            {
                parts.Add($"{pair.Key}={pair.Value}");
            }

            if (TextSimilarity(historical.InnerText, current.InnerText) >= 0.8)
            {
                parts.Add("text");
            }

            if (AncestorOverlap(historicalPath, currentPath) >= 0.5)
            {
                parts.Add("ancestors");
            }

            return string.Join(", ", parts);
        }

        private static void Add(ref double score, ref double weight, double amount, bool matched)
        {
            Add(ref score, ref weight, amount, matched ? 1d : 0d);
        }

        private static void Add(ref double score, ref double weight, double amount, double ratio)
        {
            weight += amount;
            score += amount * Math.Clamp(ratio, 0, 1);
        }

        private static bool TagsEqual(string? left, string? right) =>
            !string.IsNullOrWhiteSpace(left) &&
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static bool IdsEqual(string? left, string? right) =>
            !string.IsNullOrWhiteSpace(left) &&
            string.Equals(left, right, StringComparison.Ordinal);

        private static double ClassOverlap(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
        {
            var a = (left ?? Array.Empty<string>()).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var b = (right ?? Array.Empty<string>()).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (a.Count == 0 && b.Count == 0)
            {
                return 0.25;
            }

            if (a.Count == 0 || b.Count == 0)
            {
                return 0;
            }

            return (double)a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count() / a.Union(b, StringComparer.OrdinalIgnoreCase).Count();
        }

        private static double AttributeOverlap(Dictionary<string, string>? left, Dictionary<string, string>? right)
        {
            var a = left ?? new Dictionary<string, string>();
            var b = right ?? new Dictionary<string, string>();
            var historicalKeys = a.Keys.ToList();
            if (historicalKeys.Count == 0)
            {
                return b.Count == 0 ? 0.25 : 0;
            }

            var matches = historicalKeys.Count(key =>
                b.TryGetValue(key, out var value) &&
                string.Equals(a[key], value, StringComparison.Ordinal));
            return (double)matches / historicalKeys.Count;
        }

        private static List<KeyValuePair<string, string>> SharedAttributes(
            Dictionary<string, string>? left,
            Dictionary<string, string>? right)
        {
            var shared = new List<KeyValuePair<string, string>>();
            if (left == null || right == null)
            {
                return shared;
            }

            foreach (var pair in left)
            {
                if (right.TryGetValue(pair.Key, out var value) &&
                    string.Equals(pair.Value, value, StringComparison.Ordinal))
                {
                    shared.Add(pair);
                }
            }

            return shared;
        }

        private static double TextSimilarity(string? left, string? right)
        {
            var a = NormalizeText(left);
            var b = NormalizeText(right);
            if (a.Length == 0 && b.Length == 0)
            {
                return 0.25;
            }

            if (a.Length == 0 || b.Length == 0)
            {
                return 0;
            }

            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase))
            {
                return 0.7;
            }

            return 0;
        }

        private static double AncestorOverlap(IReadOnlyList<HealeniumNode>? historicalPath, IReadOnlyList<HealeniumNode>? currentPath)
        {
            var historical = TakeAncestors(historicalPath);
            var current = TakeAncestors(currentPath);
            if (historical.Count == 0 || current.Count == 0)
            {
                return 0.25;
            }

            var matches = historical.Intersect(current, StringComparer.OrdinalIgnoreCase).Count();
            return (double)matches / Math.Max(historical.Count, current.Count);
        }

        private static List<string> TakeAncestors(IReadOnlyList<HealeniumNode>? path)
        {
            if (path == null || path.Count < 2)
            {
                return new List<string>();
            }

            return path
                .Take(Math.Max(0, path.Count - 1))
                .TakeLast(3)
                .Select(node => $"{node.Tag ?? "*"}#{node.Id ?? string.Empty}")
                .ToList();
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return trimmed.Length <= 80 ? trimmed : trimmed[..80];
        }
    }
}

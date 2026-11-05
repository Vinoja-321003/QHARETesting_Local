using System.Diagnostics;
using System.Linq;
using OpenQA.Selenium;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Maps Selenium <see cref="By"/> locators to Healenium type/value pairs and candidate locators.
    /// </summary>
    public static class HealeniumLocatorMapper
    {
        public static (string Type, string Value) Split(By locator)
        {
            var text = locator.ToString();
            var parts = text.Split(':', 2);
            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }

            return ("By.CssSelector", text);
        }

        /// <summary>
        /// Parses Healenium report/list locator strings such as <c>By.CssSelector(body#sauce-demo)</c>.
        /// </summary>
        public static (string Type, string Value) ParseReportedLocator(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return ("By.CssSelector", string.Empty);
            }

            var trimmed = text.Trim();
            var open = trimmed.IndexOf('(');
            var close = trimmed.LastIndexOf(')');
            if (open > 0 && close > open)
            {
                return (trimmed[..open].Trim(), trimmed[(open + 1)..close]);
            }

            var parts = trimmed.Split(':', 2);
            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }

            return ("By.CssSelector", trimmed);
        }

        public const double RelatedLocatorMinSimilarity = 0.45;

        public static double LocatorSimilarity(string typeA, string valueA, string typeB, string valueB)
        {
            var a = valueA ?? string.Empty;
            var b = valueB ?? string.Empty;
            if (a.Length == 0 || b.Length == 0)
            {
                return 0;
            }

            var typeFactor = string.Equals(NormalizeType(typeA), NormalizeType(typeB), StringComparison.OrdinalIgnoreCase)
                ? 1.0
                : 0.75;

            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0 * typeFactor;
            }

            if (a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase))
            {
                return 0.92 * typeFactor;
            }

            var prefix = 0;
            var limit = Math.Min(a.Length, b.Length);
            while (prefix < limit && char.ToLowerInvariant(a[prefix]) == char.ToLowerInvariant(b[prefix]))
            {
                prefix++;
            }

            var prefixRatio = (double)prefix / Math.Max(a.Length, b.Length);
            var distance = Levenshtein(a.ToLowerInvariant(), b.ToLowerInvariant());
            var editRatio = 1.0 - ((double)distance / Math.Max(a.Length, b.Length));
            var jaccard = TokenJaccard(a, b);
            return Math.Max(Math.Max(prefixRatio, editRatio), jaccard) * typeFactor;
        }

        public static IReadOnlyList<(double Similarity, By Locator, HealeniumStoredSelector Source)> RankRelatedSelectors(
            IEnumerable<HealeniumStoredSelector> selectors,
            string locatorType,
            string locatorValue,
            string url)
        {
            var page = HealeniumEvidenceStore.NormalizeUrl(url);
            var ranked = new List<(double Similarity, By Locator, HealeniumStoredSelector Source)>();
            foreach (var selector in selectors)
            {
                if (!string.Equals(HealeniumEvidenceStore.NormalizeUrl(selector.Url), page, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var (storedType, storedValue) = ParseReportedLocator(selector.Locator);
                if (string.IsNullOrWhiteSpace(storedValue))
                {
                    continue;
                }

                if (string.Equals(storedValue, locatorValue, StringComparison.Ordinal) &&
                    string.Equals(NormalizeType(storedType), NormalizeType(locatorType), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var similarity = LocatorSimilarity(locatorType, locatorValue, storedType, storedValue);
                if (similarity < RelatedLocatorMinSimilarity)
                {
                    continue;
                }

                ranked.Add((similarity, FromTypeAndValue(storedType, storedValue), selector));
            }

            return ranked
                .OrderByDescending(item => item.Similarity)
                .ToList();
        }

        private static int Levenshtein(string left, string right)
        {
            var n = left.Length;
            var m = right.Length;
            var prev = new int[m + 1];
            var curr = new int[m + 1];
            for (var j = 0; j <= m; j++)
            {
                prev[j] = j;
            }

            for (var i = 1; i <= n; i++)
            {
                curr[0] = i;
                for (var j = 1; j <= m; j++)
                {
                    var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }

                (prev, curr) = (curr, prev);
            }

            return prev[m];
        }

        private static double TokenJaccard(string left, string right)
        {
            var a = Tokens(left);
            var b = Tokens(right);
            if (a.Count == 0 || b.Count == 0)
            {
                return 0;
            }

            var intersection = a.Intersect(b, StringComparer.Ordinal).Count();
            var union = a.Union(b, StringComparer.Ordinal).Count();
            return union == 0 ? 0 : (double)intersection / union;
        }

        private static HashSet<string> Tokens(string value)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            var current = new System.Text.StringBuilder();
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    current.Append(char.ToLowerInvariant(ch));
                    continue;
                }

                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }

        public static HealeniumLocatorDto ToDto(By locator)
        {
            var (type, value) = Split(locator);
            return new HealeniumLocatorDto { Type = type, Value = value };
        }

        public static By FromDto(HealeniumLocatorDto dto)
        {
            return FromTypeAndValue(dto.Type, dto.Value);
        }

        public static By FromTypeAndValue(string type, string value)
        {
            var strategy = (type ?? string.Empty).Replace("By.", "", StringComparison.OrdinalIgnoreCase).Trim().ToLowerInvariant();
            value ??= string.Empty;

            return strategy switch
            {
                "id" => By.Id(value),
                "name" => By.Name(value),
                "classname" or "class" => By.ClassName(value),
                "cssselector" or "css" => By.CssSelector(value),
                "xpath" => By.XPath(value),
                "tagname" or "tag" => By.TagName(value),
                "linktext" => By.LinkText(value),
                "partiallinktext" => By.PartialLinkText(value),
                _ => By.CssSelector(value)
            };
        }

        public static (string ClassName, string MethodName) OriginCaller()
        {
            var frames = new StackTrace(true).GetFrames() ?? Array.Empty<StackFrame>();
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                var type = method?.DeclaringType;
                if (type == null || method == null)
                {
                    continue;
                }

                var fullName = type.FullName ?? type.Name;
                if (                    fullName.StartsWith("QualiFrame.Framework.Utilities.WebDriver", StringComparison.Ordinal) ||
                    fullName.StartsWith("QualiFrame.Framework.PageBases", StringComparison.Ordinal) ||
                    fullName.StartsWith("System.", StringComparison.Ordinal) ||
                    fullName.StartsWith("OpenQA.Selenium", StringComparison.Ordinal) ||
                    fullName.StartsWith("SeleniumExtras.", StringComparison.Ordinal) ||
                    fullName.StartsWith("NUnit.", StringComparison.Ordinal) ||
                    fullName.StartsWith("Reqnroll.", StringComparison.Ordinal))
                {
                    continue;
                }

                return (fullName, method.Name);
            }

            return ("QualiFrame.Tests", "FindElement");
        }

        public static IReadOnlyList<By> CandidatesFromNode(HealeniumNode node, int max)
        {
            var candidates = new List<By>();
            var tag = string.IsNullOrWhiteSpace(node.Tag) ? "*" : node.Tag.Trim();
            var classes = (node.Classes ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

            if (!string.IsNullOrWhiteSpace(node.Id))
            {
                Add(candidates, By.Id(node.Id));
                Add(candidates, By.CssSelector($"#{CssEscape(node.Id)}"));
                Add(candidates, By.CssSelector($"{tag}#{CssEscape(node.Id)}"));
            }

            if (node.Other != null && node.Other.TryGetValue("name", out var name) && !string.IsNullOrWhiteSpace(name))
            {
                Add(candidates, By.Name(name));
                Add(candidates, By.CssSelector($"{tag}[name='{CssAttr(name)}']"));
            }

            if (node.Other != null && node.Other.TryGetValue("type", out var inputType) && !string.IsNullOrWhiteSpace(inputType))
            {
                Add(candidates, By.CssSelector($"{tag}[type='{CssAttr(inputType)}']"));
            }

            if (classes.Count > 0)
            {
                var classSelector = string.Join(".", classes.Select(CssEscape));
                Add(candidates, By.CssSelector($"{tag}.{classSelector}"));
            }

            if (node.Other != null && node.Other.TryGetValue("data-testid", out var testId) && !string.IsNullOrWhiteSpace(testId))
            {
                Add(candidates, By.CssSelector($"[data-testid='{CssAttr(testId)}']"));
            }

            var text = node.InnerText?.Trim();
            if (!string.IsNullOrEmpty(text) && text.Length <= 80 && !text.Contains('\n'))
            {
                Add(candidates, By.XPath($"//{tag}[contains(normalize-space(.),'{XpathLiteral(text)}')]"));
            }

            if (string.Equals(tag, "a", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(text) && text.Length <= 80)
            {
                Add(candidates, By.LinkText(text));
                Add(candidates, By.PartialLinkText(text.Length > 20 ? text[..20] : text));
            }

            return candidates.Take(Math.Max(1, max)).ToList();
        }

        public static bool SameLocator(HealeniumLocatorDto left, By right)
        {
            var dto = ToDto(right);
            return string.Equals(left.Value, dto.Value, StringComparison.Ordinal) &&
                   string.Equals(NormalizeType(left.Type), NormalizeType(dto.Type), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeType(string type) =>
            (type ?? string.Empty).Replace("By.", "", StringComparison.OrdinalIgnoreCase).Trim();

        private static void Add(List<By> list, By locator)
        {
            var key = locator.ToString();
            if (list.All(existing => existing.ToString() != key))
            {
                list.Add(locator);
            }
        }

        private static string CssEscape(string value) =>
            value.Replace("\\", "\\\\").Replace(".", "\\.");

        private static string CssAttr(string value) =>
            value.Replace("'", "\\'");

        private static string XpathLiteral(string value) =>
            value.Replace("'", "',\"'\",'");
    }
}

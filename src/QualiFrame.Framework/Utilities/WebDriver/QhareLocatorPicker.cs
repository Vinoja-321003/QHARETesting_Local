using OpenQA.Selenium;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Maps QHARE locator types onto Selenium <see cref="By"/> using the project manifest order.
    /// </summary>
    public static class QhareLocatorPicker
    {
        public static By Pick(QhareHealResponse? response, IReadOnlyList<string>? strategies)
        {
            var locators = Collect(response);
            if (locators.Count == 0)
            {
                return By.CssSelector("*");
            }

            if (strategies is { Count: > 0 })
            {
                foreach (var strategy in strategies)
                {
                    foreach (var locator in locators)
                    {
                        if (string.Equals(strategy, locator.Type, StringComparison.OrdinalIgnoreCase))
                        {
                            return ToBy(locator.Type, locator.Value ?? locator.Name);
                        }
                    }
                }
            }

            return ToBy(locators[0].Type, locators[0].Value ?? locators[0].Name);
        }

        public static By ToBy(string? type, string? value)
        {
            var kind = (type ?? "css").ToLowerInvariant();
            var selector = string.IsNullOrWhiteSpace(value) ? "*" : value;
            return kind switch
            {
                "id" => By.Id(selector.StartsWith('#') ? selector[1..] : selector),
                "name" => By.Name(selector),
                "xpath" => By.XPath(selector),
                "testid" => By.CssSelector($"[data-testid='{CssAttr(selector)}']"),
                "classname" or "class" => By.ClassName(selector),
                "linktext" => By.LinkText(selector),
                "partiallinktext" => By.PartialLinkText(selector),
                _ => By.CssSelector(selector)
            };
        }

        public static (string Type, string Value) Describe(By by)
        {
            var text = by.ToString() ?? string.Empty;
            var split = text.IndexOf(':');
            if (split < 0)
            {
                return ("css", text);
            }

            var mechanism = text[..split].Replace("By.", "", StringComparison.Ordinal).Trim().ToLowerInvariant();
            var value = text[(split + 1)..].Trim();
            var type = mechanism switch
            {
                "id" => "id",
                "name" => "name",
                "xpath" => "xpath",
                "classname" or "class name" => "css",
                "cssselector" or "css selector" or "css" => "css",
                "linktext" or "link text" => "css",
                "partiallinktext" or "partial link text" => "css",
                "tagname" or "tag name" => "css",
                _ => "css"
            };
            return (type, value);
        }

        public static string LogicalName(By by, string? className, string? methodName)
        {
            var (_, value) = Describe(by);
            var token = Sanitize(string.IsNullOrWhiteSpace(value) ? "element" : value);
            var page = Sanitize(ShortType(className));
            var method = Sanitize(methodName);
            if (!string.IsNullOrEmpty(method) && !IsInfrastructureMethod(method))
            {
                return string.IsNullOrEmpty(page) ? method : $"{page}.{method}";
            }

            return string.IsNullOrEmpty(page) ? token : $"{page}.{token}";
        }

        private static bool IsInfrastructureMethod(string method) =>
            method.Equals("FindElement", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("FindElements", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("WaitForElementToBeVisible", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("WaitForElementToBePresent", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("WaitForElementToBeClickable", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("IsElementDisplayed", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("ClickElement", StringComparison.OrdinalIgnoreCase);

        private static List<QhareLocator> Collect(QhareHealResponse? response)
        {
            var found = new List<QhareLocator>();
            var candidate = response?.Candidate;
            if (candidate?.Locators != null)
            {
                found.AddRange(candidate.Locators.Where(IsUsable));
            }

            if (found.Count == 0 && candidate?.PreferredLocator != null && IsUsable(candidate.PreferredLocator))
            {
                found.Add(candidate.PreferredLocator);
            }

            return found;
        }

        private static bool IsUsable(QhareLocator locator) =>
            !string.IsNullOrWhiteSpace(locator.Type) &&
            (!string.IsNullOrWhiteSpace(locator.Value) || !string.IsNullOrWhiteSpace(locator.Name));

        private static string CssAttr(string value) =>
            value.Replace("'", "\\'");

        private static string ShortType(string? className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return string.Empty;
            }

            var last = className.LastIndexOf('.');
            return last >= 0 ? className[(last + 1)..] : className;
        }

        private static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Trim().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars).Trim('_');
        }
    }
}

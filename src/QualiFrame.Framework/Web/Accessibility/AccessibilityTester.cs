using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using Selenium.Axe;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Web.Accessibility
{
    /// <summary>
    /// Accessibility testing using axe-core for WCAG compliance checking.
    /// </summary>
    public class AccessibilityTester
    {
        private readonly IWebDriver _driver;
        private readonly TestConfiguration _config;
        private readonly AxeBuilder _axeBuilder;

        public AccessibilityTester(IWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _config = TestConfiguration.Instance;
            _axeBuilder = new AxeBuilder(_driver);

            var wcagLevel = _config.AccessibilityWCAGLevel?.ToUpper() ?? "AA";
            switch (wcagLevel)
            {
                case "A":
                    _axeBuilder.WithTags("wcag2a");
                    break;
                case "AA":
                    _axeBuilder.WithTags("wcag2aa", "wcag21aa");
                    break;
                case "AAA":
                    _axeBuilder.WithTags("wcag2aaa", "wcag21aaa");
                    break;
                default:
                    _axeBuilder.WithTags("wcag2aa", "wcag21aa");
                    break;
            }

            if (_config.AccessibilityIncludeBestPractices)
            {
                _axeBuilder.WithTags("best-practice");
            }
        }

        /// <summary>
        /// Runs an accessibility check on the current page.
        /// </summary>
        public AccessibilityResults RunAccessibilityCheck()
        {
            try
            {
                Logger.Information("Running accessibility check on current page");
                var axeResult = _axeBuilder.Analyze();

                var results = new AccessibilityResults
                {
                    Violations = ConvertViolations(axeResult.Violations),
                    Passes = ConvertPasses(axeResult.Passes),
                    Incomplete = ConvertIncomplete(axeResult.Incomplete),
                    Inapplicable = ConvertInapplicable(axeResult.Inapplicable),
                    Url = _driver.Url,
                    Timestamp = DateTime.UtcNow
                };

                Logger.Information("Accessibility check completed. Violations: {Violations}, Passes: {Passes}",
                    results.Violations.Count, results.Passes.Count);

                return results;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to run accessibility check");
                throw;
            }
        }

        /// <summary>
        /// Runs an accessibility check on a specific element.
        /// </summary>
        public AccessibilityResults RunAccessibilityCheck(IWebElement element)
        {
            try
            {
                Logger.Information("Running accessibility check on specific element");
                var axeResult = _axeBuilder.Analyze(element);

                var results = new AccessibilityResults
                {
                    Violations = ConvertViolations(axeResult.Violations),
                    Passes = ConvertPasses(axeResult.Passes),
                    Incomplete = ConvertIncomplete(axeResult.Incomplete),
                    Inapplicable = ConvertInapplicable(axeResult.Inapplicable),
                    Url = _driver.Url,
                    Timestamp = DateTime.UtcNow
                };

                return results;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to run accessibility check on element");
                throw;
            }
        }

        /// <summary>
        /// Asserts that there are no accessibility violations.
        /// </summary>
        public void AssertNoViolations(AccessibilityResults results)
        {
            if (results.Violations.Count > 0)
            {
                var violationMessages = string.Join("\n", 
                    results.Violations.Select(v => $"- {v.Id}: {v.Description} ({v.Impact})"));
                
                throw new AccessibilityViolationException(
                    $"Found {results.Violations.Count} accessibility violation(s):\n{violationMessages}",
                    results);
            }
        }

        /// <summary>
        /// Asserts that there are no critical accessibility violations.
        /// </summary>
        public void AssertNoCriticalViolations(AccessibilityResults results)
        {
            var criticalViolations = results.Violations.Where(v => 
                v.Impact == "critical" || v.Impact == "serious").ToList();

            if (criticalViolations.Count > 0)
            {
                var violationMessages = string.Join("\n", 
                    criticalViolations.Select(v => $"- {v.Id}: {v.Description} ({v.Impact})"));
                
                throw new AccessibilityViolationException(
                    $"Found {criticalViolations.Count} critical accessibility violation(s):\n{violationMessages}",
                    results);
            }
        }

        private List<AccessibilityViolation> ConvertViolations(dynamic violations)
        {
            var result = new List<AccessibilityViolation>();
            if (violations == null) return result;

            try
            {
                foreach (var violation in violations)
                {
                    result.Add(new AccessibilityViolation
                    {
                        Id = violation.id?.ToString() ?? string.Empty,
                        Description = violation.description?.ToString() ?? string.Empty,
                        Impact = violation.impact?.ToString(),
                        Help = violation.help?.ToString() ?? string.Empty,
                        HelpUrl = violation.helpUrl?.ToString(),
                        Nodes = ConvertNodes(violation.nodes)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error converting violations: {Error}", ex.Message);
            }

            return result;
        }

        private List<AccessibilityPass> ConvertPasses(dynamic passes)
        {
            var result = new List<AccessibilityPass>();
            if (passes == null) return result;

            try
            {
                foreach (var pass in passes)
                {
                    result.Add(new AccessibilityPass
                    {
                        Id = pass.id?.ToString() ?? string.Empty,
                        Description = pass.description?.ToString() ?? string.Empty,
                        Impact = pass.impact?.ToString(),
                        Help = pass.help?.ToString() ?? string.Empty,
                        HelpUrl = pass.helpUrl?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error converting passes: {Error}", ex.Message);
            }

            return result;
        }

        private List<AccessibilityIncomplete> ConvertIncomplete(dynamic incomplete)
        {
            var result = new List<AccessibilityIncomplete>();
            if (incomplete == null) return result;

            try
            {
                foreach (var item in incomplete)
                {
                    result.Add(new AccessibilityIncomplete
                    {
                        Id = item.id?.ToString() ?? string.Empty,
                        Description = item.description?.ToString() ?? string.Empty,
                        Impact = item.impact?.ToString(),
                        Help = item.help?.ToString() ?? string.Empty,
                        HelpUrl = item.helpUrl?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error converting incomplete: {Error}", ex.Message);
            }

            return result;
        }

        private List<AccessibilityInapplicable> ConvertInapplicable(dynamic inapplicable)
        {
            var result = new List<AccessibilityInapplicable>();
            if (inapplicable == null) return result;

            try
            {
                foreach (var item in inapplicable)
                {
                    result.Add(new AccessibilityInapplicable
                    {
                        Id = item.id?.ToString() ?? string.Empty,
                        Description = item.description?.ToString() ?? string.Empty,
                        Help = item.help?.ToString() ?? string.Empty,
                        HelpUrl = item.helpUrl?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error converting inapplicable: {Error}", ex.Message);
            }

            return result;
        }

        private List<AccessibilityNode> ConvertNodes(dynamic? nodes)
        {
            var result = new List<AccessibilityNode>();
            if (nodes == null) return result;

            try
            {
                foreach (var node in nodes)
                {
                    result.Add(new AccessibilityNode
                    {
                        Html = node.html?.ToString() ?? string.Empty,
                        Target = node.target != null ? ((IEnumerable<object>)node.target).Select(t => t.ToString() ?? string.Empty).ToList() : new List<string>(),
                        FailureSummary = node.failureSummary?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error converting nodes: {Error}", ex.Message);
            }

            return result;
        }
    }

    /// <summary>
    /// Accessibility test results.
    /// </summary>
    public class AccessibilityResults
    {
        public List<AccessibilityViolation> Violations { get; set; } = new();
        public List<AccessibilityPass> Passes { get; set; } = new();
        public List<AccessibilityIncomplete> Incomplete { get; set; } = new();
        public List<AccessibilityInapplicable> Inapplicable { get; set; } = new();
        public string Url { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Accessibility violation.
    /// </summary>
    public class AccessibilityViolation
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Impact { get; set; }
        public string Help { get; set; } = string.Empty;
        public string? HelpUrl { get; set; }
        public List<AccessibilityNode> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Accessibility pass.
    /// </summary>
    public class AccessibilityPass
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Impact { get; set; }
        public string Help { get; set; } = string.Empty;
        public string? HelpUrl { get; set; }
    }

    /// <summary>
    /// Accessibility incomplete check.
    /// </summary>
    public class AccessibilityIncomplete
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Impact { get; set; }
        public string Help { get; set; } = string.Empty;
        public string? HelpUrl { get; set; }
    }

    /// <summary>
    /// Accessibility inapplicable check.
    /// </summary>
    public class AccessibilityInapplicable
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string? HelpUrl { get; set; }
    }

    /// <summary>
    /// Accessibility node (element with violation).
    /// </summary>
    public class AccessibilityNode
    {
        public string Html { get; set; } = string.Empty;
        public List<string> Target { get; set; } = new();
        public string? FailureSummary { get; set; }
    }

    /// <summary>
    /// Exception thrown when accessibility violations are found.
    /// </summary>
    public class AccessibilityViolationException : Exception
    {
        public AccessibilityResults Results { get; }

        public AccessibilityViolationException(string message, AccessibilityResults results) 
            : base(message)
        {
            Results = results;
        }
    }
}

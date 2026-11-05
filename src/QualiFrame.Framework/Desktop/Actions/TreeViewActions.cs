using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Provides reusable high-level actions for interacting with desktop tree controls.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class TreeViewActions
    {
        public static void ExpandNode(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.ExpandNode started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.ExpandNode - Path={nodePath}, Description={description}");

            SmartDesktopActions.Retry(() => WalkPath(Resolve(treeRoot), nodePath, expand: true));
        }

        public static void CollapseNode(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.CollapseNode started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.CollapseNode - Path={nodePath}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var node = WalkPath(Resolve(treeRoot), nodePath, expand: true);
                if (node.Patterns.ExpandCollapse.TryGetPattern(out var pattern))
                {
                    pattern.Collapse();
                }
                else
                {
                    node.Click();
                }
            });
        }

        public static void SelectNode(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.SelectNode started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.SelectNode - Path={nodePath}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var node = WalkPath(Resolve(treeRoot), nodePath, expand: true);
                Select(node);
            });
        }

        public static void SelectNodeByText(DesktopElement treeRoot, string text, string? description = null)
        {
            Logger.Information("TreeViewActions.SelectNodeByText started. Text={Text}, Description={Description}", text, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.SelectNodeByText - Text={text}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var node = FindByText(Resolve(treeRoot), text)
                    ?? throw new InvalidOperationException($"Tree node with text '{text}' was not found.");
                Select(node);
            });
        }

        public static string GetNodeText(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.GetNodeText started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.GetNodeText - Path={nodePath}, Description={description}");

            string text = string.Empty;
            SmartDesktopActions.Retry(() =>
            {
                text = DesktopUiQuery.ReadText(WalkPath(Resolve(treeRoot), nodePath, expand: true));
            });
            return text;
        }

        public static IReadOnlyList<string> GetChildNodes(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.GetChildNodes started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.GetChildNodes - Path={nodePath}, Description={description}");

            IReadOnlyList<string> names = Array.Empty<string>();
            SmartDesktopActions.Retry(() =>
            {
                var node = WalkPath(Resolve(treeRoot), nodePath, expand: true);
                Expand(node);
                names = DesktopUiQuery.Names(DesktopUiQuery.ByControlType(node, ControlType.TreeItem)).ToList();
            });
            return names;
        }

        public static string? GetParentNode(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.GetParentNode started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.GetParentNode - Path={nodePath}, Description={description}");

            var segments = SplitPath(nodePath);
            if (segments.Length <= 1)
            {
                return null;
            }

            return string.Join("/", segments.Take(segments.Length - 1));
        }

        public static bool IsNodeExpanded(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.IsNodeExpanded started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.IsNodeExpanded - Path={nodePath}, Description={description}");

            var expanded = false;
            SmartDesktopActions.Retry(() =>
            {
                var node = WalkPath(Resolve(treeRoot), nodePath, expand: false);
                expanded = node.Patterns.ExpandCollapse.TryGetPattern(out var pattern) &&
                           pattern.ExpandCollapseState == ExpandCollapseState.Expanded;
            });
            return expanded;
        }

        public static bool IsNodeVisible(DesktopElement treeRoot, string nodePath, string? description = null)
        {
            Logger.Information("TreeViewActions.IsNodeVisible started. Path={Path}, Description={Description}", nodePath, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.IsNodeVisible - Path={nodePath}, Description={description}");

            try
            {
                var node = WalkPath(Resolve(treeRoot), nodePath, expand: true);
                return node.IsAvailable && !node.IsOffscreen;
            }
            catch
            {
                return false;
            }
        }

        public static string? FindNodeRecursively(DesktopElement treeRoot, string text, string? description = null)
        {
            Logger.Information("TreeViewActions.FindNodeRecursively started. Text={Text}, Description={Description}", text, description ?? string.Empty);
            ExtentReportManager.LogInfo($"TreeViewActions.FindNodeRecursively - Text={text}, Description={description}");

            var match = FindByText(Resolve(treeRoot), text);
            return match == null ? null : DesktopUiQuery.ReadText(match);
        }

        private static AutomationElement Resolve(DesktopElement treeRoot) =>
            SelfHealingLocatorEngine.FindElement(treeRoot.Session, treeRoot.Locator);

        private static AutomationElement WalkPath(AutomationElement root, string nodePath, bool expand)
        {
            var segments = SplitPath(nodePath);
            AutomationElement current = root;
            foreach (var segment in segments)
            {
                var node = DesktopUiQuery.ByName(current, segment)
                           ?? FindByText(current, segment)
                           ?? throw new InvalidOperationException($"Tree node '{segment}' was not found under path '{nodePath}'.");
                if (expand)
                {
                    Expand(node);
                }

                current = node;
            }

            return current;
        }

        private static AutomationElement? FindByText(AutomationElement root, string text)
        {
            var items = DesktopUiQuery.ByControlType(root, ControlType.TreeItem);
            return items.FirstOrDefault(i =>
                string.Equals(DesktopUiQuery.ReadText(i).Trim(), text, StringComparison.OrdinalIgnoreCase));
        }

        private static void Expand(AutomationElement node)
        {
            if (node.Patterns.ExpandCollapse.TryGetPattern(out var pattern) &&
                pattern.ExpandCollapseState != ExpandCollapseState.Expanded)
            {
                pattern.Expand();
            }
        }

        private static void Select(AutomationElement node)
        {
            if (node.Patterns.SelectionItem.TryGetPattern(out var selection))
            {
                selection.Select();
            }
            else
            {
                node.Click();
            }
        }

        private static string[] SplitPath(string nodePath) =>
            (nodePath ?? string.Empty).Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
    }
}

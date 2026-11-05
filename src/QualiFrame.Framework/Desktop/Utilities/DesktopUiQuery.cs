using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace QualiFrame.Framework.Desktop.Utilities
{
    /// <summary>
    /// Shared FlaUI lookup helpers used by action libraries.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class DesktopUiQuery
    {
        public static AutomationElement? ByName(AutomationElement root, string name, bool descendants = true)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            try
            {
                return descendants
                    ? root.FindFirstDescendant(cf => cf.ByName(name))
                    : root.FindFirstChild(cf => cf.ByName(name));
            }
            catch
            {
                return null;
            }
        }

        public static AutomationElement? ByClassName(AutomationElement root, string className, bool descendants = true)
        {
            if (root == null || string.IsNullOrWhiteSpace(className))
            {
                return null;
            }

            try
            {
                return descendants
                    ? root.FindFirstDescendant(cf => cf.ByClassName(className))
                    : root.FindFirstChild(cf => cf.ByClassName(className));
            }
            catch
            {
                return null;
            }
        }

        public static AutomationElement? ByAutomationId(AutomationElement root, string automationId, bool descendants = true)
        {
            if (root == null || string.IsNullOrWhiteSpace(automationId))
            {
                return null;
            }

            try
            {
                return descendants
                    ? root.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
                    : root.FindFirstChild(cf => cf.ByAutomationId(automationId));
            }
            catch
            {
                return null;
            }
        }

        public static AutomationElement[] ByControlType(AutomationElement root, ControlType controlType)
        {
            if (root == null)
            {
                return Array.Empty<AutomationElement>();
            }

            try
            {
                return root.FindAllDescendants(cf => cf.ByControlType(controlType)) ?? Array.Empty<AutomationElement>();
            }
            catch
            {
                return Array.Empty<AutomationElement>();
            }
        }

        public static AutomationElement? FirstByNames(AutomationElement root, params string[] names)
        {
            foreach (var name in names)
            {
                var match = ByName(root, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        public static IEnumerable<string> Names(IEnumerable<AutomationElement> elements)
        {
            foreach (var element in elements)
            {
                string? name = null;
                try
                {
                    name = element.Name;
                }
                catch
                {
                    // ignored
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return name;
                }
            }
        }

        public static string ReadText(AutomationElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            try
            {
                if (element.Patterns.Value.TryGetPattern(out var valuePattern) && valuePattern.Value != null)
                {
                    return valuePattern.Value;
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                if (element.Patterns.Text.TryGetPattern(out var textPattern))
                {
                    var text = textPattern.DocumentRange?.GetText(int.MaxValue);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                return element.Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool TryInvoke(AutomationElement element)
        {
            try
            {
                if (element.Patterns.Invoke.TryGetPattern(out var invoke))
                {
                    invoke.Invoke();
                    return true;
                }
            }
            catch
            {
                // fall through to click
            }

            try
            {
                element.Click();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void SetValue(AutomationElement element, string value, bool clearFirst)
        {
            element.Focus();

            if (element.Patterns.Value.TryGetPattern(out var valuePattern) && !valuePattern.IsReadOnly)
            {
                if (clearFirst)
                {
                    valuePattern.SetValue(string.Empty);
                }

                valuePattern.SetValue(clearFirst ? value : (valuePattern.Value ?? string.Empty) + value);
                return;
            }

            try
            {
                var textBox = element.AsTextBox();
                if (clearFirst)
                {
                    textBox.Text = value;
                }
                else
                {
                    textBox.Enter(value);
                }

                return;
            }
            catch
            {
                // keyboard fallback
            }

            if (clearFirst)
            {
                FlaUI.Core.Input.Keyboard.TypeSimultaneously(
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
                FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
            }

            FlaUI.Core.Input.Keyboard.Type(value);
        }
    }
}

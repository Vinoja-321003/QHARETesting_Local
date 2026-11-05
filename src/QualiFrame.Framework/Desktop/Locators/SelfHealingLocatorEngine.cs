using System;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Locators
{
    /// <summary>
    /// Provides self-healing element lookup for Windows desktop automation via FlaUI.
    /// Tries multiple locator strategies (AutomationId → Name → ClassName → ControlType) with logging.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class SelfHealingLocatorEngine
    {
        public static AutomationElement FindElement(DesktopSession session, LocatorDefinition locator)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (locator == null) throw new ArgumentNullException(nameof(locator));

            var root = session.SearchRoot ?? throw new InvalidOperationException("Desktop session has no current window to search.");

            var automationId = locator.ResolvedAutomationId;
            if (!string.IsNullOrWhiteSpace(automationId))
            {
                try
                {
                    Logger.Debug("Trying locator by AutomationId: {Id}", automationId);
                    var element = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                    if (element != null)
                    {
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("AutomationId locator failed for {Id}: {Error}", automationId, ex.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(locator.Name))
            {
                try
                {
                    Logger.Debug("Trying locator by Name: {Name}", locator.Name);
                    var element = locator.ControlType is { } controlType &&
                                  TryParseControlType(controlType, out var parsed)
                        ? root.FindFirstDescendant(cf => cf.ByName(locator.Name).And(cf.ByControlType(parsed)))
                        : root.FindFirstDescendant(cf => cf.ByName(locator.Name));
                    if (element != null)
                    {
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("Name locator failed for {Name}: {Error}", locator.Name, ex.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(locator.ClassName))
            {
                try
                {
                    Logger.Debug("Trying locator by ClassName: {ClassName}", locator.ClassName);
                    var element = root.FindFirstDescendant(cf => cf.ByClassName(locator.ClassName));
                    if (element != null)
                    {
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("ClassName locator failed for {ClassName}: {Error}", locator.ClassName, ex.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(locator.ControlType) &&
                TryParseControlType(locator.ControlType, out var control))
            {
                try
                {
                    Logger.Debug("Trying locator by ControlType: {ControlType}", locator.ControlType);
                    var element = root.FindFirstDescendant(cf => cf.ByControlType(control));
                    if (element != null)
                    {
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("ControlType locator failed for {ControlType}: {Error}", locator.ControlType, ex.Message);
                }
            }

            var message =
                $"Element not found using self-healing locators. " +
                $"AutomationId='{automationId}', Name='{locator.Name}', ClassName='{locator.ClassName}', ControlType='{locator.ControlType}'.";

            Logger.Error(message);
            throw new InvalidOperationException(message);
        }

        private static bool TryParseControlType(string value, out ControlType controlType)
        {
            return Enum.TryParse(value, ignoreCase: true, out controlType);
        }
    }
}

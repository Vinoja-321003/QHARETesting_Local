using System;
using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Actions;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;

namespace QualiFrame.Framework.Desktop.Elements
{
    /// <summary>
    /// Wrapper around Windows desktop elements that adds self-healing locators,
    /// smart retry logic, and a single interaction surface.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DesktopElement
    {
        private readonly DesktopSession _session;
        private readonly LocatorDefinition _locator;

        public DesktopElement(DesktopSession session, LocatorDefinition locator)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        }

        internal DesktopSession Session => _session;
        internal LocatorDefinition Locator => _locator;

        public void Click()
        {
            SmartDesktopActions.Retry(() =>
            {
                var element = SelfHealingLocatorEngine.FindElement(_session, _locator);
                if (!DesktopUiQuery.TryInvoke(element))
                {
                    throw new InvalidOperationException(
                        $"Unable to click element AutomationId='{_locator.ResolvedAutomationId}', Name='{_locator.Name}'.");
                }
            });
        }

        public string GetText()
        {
            var element = SelfHealingLocatorEngine.FindElement(_session, _locator);
            return DesktopUiQuery.ReadText(element);
        }
    }
}

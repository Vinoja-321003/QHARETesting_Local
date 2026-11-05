using System;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using QualiFrame.Framework.Desktop.Drivers;

namespace QualiFrame.Framework.Desktop.Pages
{
    /// <summary>
    /// Base class for Windows desktop window/page objects.
    /// Provides common element finding and wait helpers on top of a FlaUI <see cref="DesktopSession"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public abstract class DesktopPage
    {
        protected DesktopSession Session { get; }

        protected DesktopPage(DesktopSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        protected AutomationElement FindByAccessibilityId(string id)
        {
            var element = Session.SearchRoot.FindFirstDescendant(cf => cf.ByAutomationId(id));
            if (element == null)
            {
                throw new InvalidOperationException($"Desktop element with AutomationId '{id}' was not found.");
            }

            return element;
        }

        protected AutomationElement FindByName(string name)
        {
            var element = Session.SearchRoot.FindFirstDescendant(cf => cf.ByName(name));
            if (element == null)
            {
                throw new InvalidOperationException($"Desktop element with Name '{name}' was not found.");
            }

            return element;
        }

        protected AutomationElement WaitUntilVisible(Func<DesktopSession, AutomationElement?> condition, int timeoutSeconds = 10)
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds));
            var result = Retry.WhileNull(
                () =>
                {
                    var element = condition(Session);
                    if (element == null)
                    {
                        return null;
                    }

                    return element.IsAvailable && !element.IsOffscreen ? element : null;
                },
                timeout,
                TimeSpan.FromMilliseconds(250),
                throwOnTimeout: false,
                ignoreException: true).Result;

            if (result == null)
            {
                throw new TimeoutException($"Desktop element was not visible within {timeoutSeconds} seconds.");
            }

            return result;
        }

        public abstract void WaitForLoaded(int timeoutSeconds = 10);
    }
}

using System;
using System.Runtime.InteropServices;

namespace QualiFrame.Framework.Desktop.AI
{
    /// <summary>
    /// Determines whether a desktop UI action should be retried based on the exception type and message.
    /// </summary>
    public static class RetryDecisionEngine
    {
        public static bool ShouldRetry(Exception ex)
        {
            if (ex is ArgumentNullException or ArgumentOutOfRangeException or ArgumentException)
            {
                return false;
            }

            if (ex is TimeoutException or COMException)
            {
                return true;
            }

            var typeName = ex.GetType().Name;
            if (typeName.Contains("FlaUI", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("ElementNot", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("PropertyNotSupported", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var message = ex.Message ?? string.Empty;
            if (message.Contains("element not interactable", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("stale element", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not available", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("offscreen", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("is disabled", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("COM", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}

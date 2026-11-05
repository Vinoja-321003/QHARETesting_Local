using System;
using System.Threading;
using QualiFrame.Framework.Desktop.AI;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Provides smart retry logic for desktop UI actions, using RetryDecisionEngine to decide when to retry.
    /// </summary>
    public static class SmartDesktopActions
    {
        public static void Retry(Action action, int retries = 5, int delayMilliseconds = 1000)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception? lastException = null;

            for (var attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (!RetryDecisionEngine.ShouldRetry(ex) || attempt == retries)
                    {
                        Logger.Error(ex, "Desktop action failed after {Attempts} attempts: {Error}", attempt, ex.Message);
                        throw;
                    }

                    Logger.Warning("Desktop action failed on attempt {Attempt}/{Retries}: {Error}. Retrying...", attempt, retries, ex.Message);
                    Thread.Sleep(delayMilliseconds);
                }
            }

            throw new InvalidOperationException("Desktop action failed after retries.", lastException);
        }
    }
}


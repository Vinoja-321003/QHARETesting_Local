using System;
using OpenTelemetry;
using OpenTelemetry.Trace;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Observability
{
    /// <summary>
    /// Opt-in OpenTelemetry console exporter. Default remains disabled so existing runs are unchanged.
    /// </summary>
    public static class TelemetryBootstrap
    {
        private static readonly object Sync = new();
        private static TracerProvider? _provider;

        public static bool IsEnabled => TestConfiguration.Instance.OpenTelemetryEnabled;

        public static void Initialize()
        {
            if (!IsEnabled)
            {
                Logger.Debug("OpenTelemetry is disabled");
                return;
            }

            lock (Sync)
            {
                if (_provider != null)
                {
                    return;
                }

                _provider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("QualiFrame")
                    .AddConsoleExporter()
                    .Build();

                Logger.Information("OpenTelemetry console exporter initialized");
            }
        }

        public static void Shutdown()
        {
            lock (Sync)
            {
                _provider?.Dispose();
                _provider = null;
            }
        }
    }
}

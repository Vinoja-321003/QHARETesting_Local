using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;
using System;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Manages the global driver pool instance.
    /// </summary>
    public static class DriverPoolManager
    {
        private static DriverPool? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets or creates the global driver pool instance.
        /// </summary>
        public static DriverPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var config = TestConfiguration.Instance;
                            _instance = new DriverPool(
                                maxPoolSize: config.MaxParallelTests,
                                minPoolSize: Math.Max(1, config.MaxParallelTests / 2),
                                driverMaxAge: TimeSpan.FromMinutes(30)
                            );
                            Logger.Information("Driver pool initialized with max size: {MaxSize}, min size: {MinSize}", 
                                config.MaxParallelTests, Math.Max(1, config.MaxParallelTests / 2));
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Disposes the global driver pool instance.
        /// </summary>
        public static void Dispose()
        {
            if (_instance != null)
            {
                lock (_lock)
                {
                    if (_instance != null)
                    {
                        _instance.Dispose();
                        _instance = null;
                        Logger.Information("Driver pool disposed");
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up the driver pool (removes old/unhealthy drivers).
        /// </summary>
        public static void Cleanup()
        {
            if (_instance != null)
            {
                _instance.Cleanup();
            }
        }
    }
}

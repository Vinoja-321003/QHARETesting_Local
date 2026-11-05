using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebDriverException = QualiFrame.Framework.Core.Exceptions.WebDriverException;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Manages a pool of WebDriver instances for parallel test execution.
    /// </summary>
    public class DriverPool : IDisposable
    {
        private readonly ConcurrentQueue<IWebDriver> _availableDrivers;
        private readonly ConcurrentDictionary<IWebDriver, DateTime> _driverCreationTimes;
        private readonly ConcurrentDictionary<IWebDriver, bool> _driverInUse;
        private readonly TestConfiguration _config;
        private readonly int _maxPoolSize;
        private readonly int _minPoolSize;
        private readonly TimeSpan _driverMaxAge;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DriverPool class.
        /// </summary>
        /// <param name="maxPoolSize">Maximum number of drivers in the pool.</param>
        /// <param name="minPoolSize">Minimum number of drivers to maintain in the pool.</param>
        /// <param name="driverMaxAge">Maximum age of a driver before it's replaced.</param>
        public DriverPool(
            int maxPoolSize = 5,
            int minPoolSize = 2,
            TimeSpan? driverMaxAge = null)
        {
            _config = TestConfiguration.Instance;
            _maxPoolSize = maxPoolSize;
            _minPoolSize = minPoolSize;
            _driverMaxAge = driverMaxAge ?? TimeSpan.FromMinutes(30);
            _availableDrivers = new ConcurrentQueue<IWebDriver>();
            _driverCreationTimes = new ConcurrentDictionary<IWebDriver, DateTime>();
            _driverInUse = new ConcurrentDictionary<IWebDriver, bool>();
            _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);

            InitializePool();
        }

        /// <summary>
        /// Gets a driver from the pool.
        /// </summary>
        /// <returns>A WebDriver instance.</returns>
        public IWebDriver GetDriver()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DriverPool));

            _semaphore.Wait();

            try
            {
                // Try to get an available driver
                if (_availableDrivers.TryDequeue(out var driver))
                {
                    // Check if driver is still healthy and not too old
                    if (DriverHealthChecker.IsHealthy(driver) && !IsDriverTooOld(driver))
                    {
                        _driverInUse[driver] = true;
                        Logger.Debug("Retrieved driver from pool. Pool size: {PoolSize}", _availableDrivers.Count);
                        return driver;
                    }
                    else
                    {
                        // Driver is unhealthy or too old, dispose it
                        DisposeDriver(driver);
                    }
                }

                // Create a new driver if pool is not at max capacity
                if (GetTotalDriverCount() < _maxPoolSize)
                {
                    var newDriver = EnhancedWebDriverFactory.CreateDriver();
                    _driverCreationTimes[newDriver] = DateTime.UtcNow;
                    _driverInUse[newDriver] = true;
                    Logger.Debug("Created new driver for pool. Total drivers: {TotalDrivers}", GetTotalDriverCount());
                    return newDriver;
                }

                // Wait for a driver to become available
                return WaitForAvailableDriver();
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                Logger.Error(ex, "Error getting driver from pool");
                throw;
            }
        }

        /// <summary>
        /// Returns a driver to the pool.
        /// </summary>
        /// <param name="driver">The driver to return.</param>
        public void ReturnDriver(IWebDriver driver)
        {
            if (_disposed || driver == null)
                return;

            try
            {
                if (_driverInUse.TryRemove(driver, out _))
                {
                    // Reset driver state if needed
                    try
                    {
                        driver.Manage().Cookies.DeleteAllCookies();
                        driver.Navigate().GoToUrl("about:blank");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Error resetting driver state before returning to pool: {Error}", ex.Message);
                    }

                    // Check if driver is still healthy
                    if (DriverHealthChecker.IsHealthy(driver) && !IsDriverTooOld(driver))
                    {
                        _availableDrivers.Enqueue(driver);
                        Logger.Debug("Returned driver to pool. Available drivers: {AvailableCount}", _availableDrivers.Count);
                    }
                    else
                    {
                        // Driver is unhealthy or too old, dispose it
                        DisposeDriver(driver);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error returning driver to pool");
                DisposeDriver(driver);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the current pool statistics.
        /// </summary>
        public DriverPoolStatistics GetStatistics()
        {
            return new DriverPoolStatistics
            {
                AvailableDrivers = _availableDrivers.Count,
                InUseDrivers = _driverInUse.Count,
                TotalDrivers = GetTotalDriverCount(),
                MaxPoolSize = _maxPoolSize,
                MinPoolSize = _minPoolSize
            };
        }

        /// <summary>
        /// Cleans up old or unhealthy drivers from the pool.
        /// </summary>
        public void Cleanup()
        {
            var driversToRemove = new List<IWebDriver>();

            // Check available drivers
            foreach (var driver in _availableDrivers)
            {
                if (!DriverHealthChecker.IsHealthy(driver) || IsDriverTooOld(driver))
                {
                    driversToRemove.Add(driver);
                }
            }

            // Remove unhealthy drivers
            foreach (var driver in driversToRemove)
            {
                if (_availableDrivers.TryDequeue(out var dequeuedDriver) && dequeuedDriver == driver)
                {
                    DisposeDriver(driver);
                }
            }

            // Ensure minimum pool size
            while (GetTotalDriverCount() < _minPoolSize && GetTotalDriverCount() < _maxPoolSize)
            {
                try
                {
                    var newDriver = EnhancedWebDriverFactory.CreateDriver();
                    _driverCreationTimes[newDriver] = DateTime.UtcNow;
                    _availableDrivers.Enqueue(newDriver);
                    Logger.Debug("Added driver to maintain minimum pool size");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error adding driver to maintain minimum pool size");
                    break;
                }
            }
        }

        private void InitializePool()
        {
            for (int i = 0; i < _minPoolSize; i++)
            {
                try
                {
                    var driver = EnhancedWebDriverFactory.CreateDriver();
                    _driverCreationTimes[driver] = DateTime.UtcNow;
                    _availableDrivers.Enqueue(driver);
                    Logger.Information("Initialized driver {Index} in pool", i + 1);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error initializing driver {Index} in pool", i + 1);
                }
            }
        }

        private IWebDriver WaitForAvailableDriver()
        {
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                if (_availableDrivers.TryDequeue(out var driver))
                {
                    if (DriverHealthChecker.IsHealthy(driver) && !IsDriverTooOld(driver))
                    {
                        _driverInUse[driver] = true;
                        return driver;
                    }
                    DisposeDriver(driver);
                }

                Thread.Sleep(100);
            }

            throw new WebDriverException("Timeout waiting for available driver from pool");
        }

        private bool IsDriverTooOld(IWebDriver driver)
        {
            if (_driverCreationTimes.TryGetValue(driver, out var creationTime))
            {
                return DateTime.UtcNow - creationTime > _driverMaxAge;
            }
            return true; // If we don't know when it was created, consider it too old
        }

        private int GetTotalDriverCount()
        {
            return _availableDrivers.Count + _driverInUse.Count;
        }

        private void DisposeDriver(IWebDriver driver)
        {
            try
            {
                _driverCreationTimes.TryRemove(driver, out _);
                _driverInUse.TryRemove(driver, out _);
                driver?.Quit();
                driver?.Dispose();
                Logger.Debug("Disposed driver from pool");
            }
            catch (Exception ex)
            {
                Logger.Warning("Error disposing driver from pool: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Disposes all drivers in the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Dispose all available drivers
            while (_availableDrivers.TryDequeue(out var driver))
            {
                DisposeDriver(driver);
            }

            // Dispose all in-use drivers
            foreach (var driver in _driverInUse.Keys.ToList())
            {
                DisposeDriver(driver);
            }

            _semaphore?.Dispose();
            Logger.Information("Driver pool disposed");
        }
    }

    /// <summary>
    /// Statistics about the driver pool.
    /// </summary>
    public class DriverPoolStatistics
    {
        /// <summary>
        /// Number of available drivers in the pool.
        /// </summary>
        public int AvailableDrivers { get; set; }

        /// <summary>
        /// Number of drivers currently in use.
        /// </summary>
        public int InUseDrivers { get; set; }

        /// <summary>
        /// Total number of drivers (available + in use).
        /// </summary>
        public int TotalDrivers { get; set; }

        /// <summary>
        /// Maximum pool size.
        /// </summary>
        public int MaxPoolSize { get; set; }

        /// <summary>
        /// Minimum pool size.
        /// </summary>
        public int MinPoolSize { get; set; }
    }
}

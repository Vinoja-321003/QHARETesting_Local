using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.Mobile
{
    /// <summary>
    /// Manages Appium server lifecycle - starting and stopping the server automatically.
    /// </summary>
    public class AppiumServerManager
    {
        private static Process? _appiumProcess;
        private static readonly TestConfiguration _config = TestConfiguration.Instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Checks if Appium server is already running at the specified URL.
        /// </summary>
        public static bool IsAppiumServerRunning(string serverUrl)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var response = client.GetAsync($"{serverUrl.TrimEnd('/')}/status").Result;
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Starts Appium server if it's not already running and AutoStartAppium is enabled.
        /// </summary>
        public static bool StartAppiumServerIfNeeded()
        {
            lock (_lock)
            {
                try
                {
                    var serverUrl = _config.AppiumServerUrl;
                    
                    // Check if auto-start is enabled
                    if (!_config.AutoStartAppium)
                    {
                        Logger.Information("AutoStartAppium is disabled. Skipping Appium server auto-start.");
                        Console.WriteLine("[INFO] AutoStartAppium is disabled. Skipping Appium server auto-start.");
                        return false;
                    }

                    // Check if server URL is localhost (only auto-start for local execution)
                    if (!IsLocalhost(serverUrl))
                    {
                        Logger.Information("AppiumServerUrl is not localhost ({ServerUrl}). Skipping auto-start (assuming remote server).", serverUrl);
                        Console.WriteLine($"[INFO] AppiumServerUrl is not localhost ({serverUrl}). Skipping auto-start (assuming remote server).");
                        return false;
                    }

                    // Check if server is already running
                    if (IsAppiumServerRunning(serverUrl))
                    {
                        Logger.Information("Appium server is already running at {ServerUrl}. Skipping auto-start.", serverUrl);
                        Console.WriteLine($"[INFO] Appium server is already running at {serverUrl}. Skipping auto-start.");
                        return true;
                    }

                    Logger.Information("Starting Appium server at {ServerUrl}...", serverUrl);
                    Console.WriteLine($"[INFO] Starting Appium server at {serverUrl}...");

                    // Extract port from URL
                    var uri = new Uri(serverUrl);
                    var port = uri.Port;

                    // Start Appium server process
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "appium",
                        Arguments = $"--port {port}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    _appiumProcess = Process.Start(startInfo);
                    if (_appiumProcess == null)
                    {
                        throw new InvalidOperationException("Failed to start Appium process. Make sure 'appium' is in your PATH.");
                    }

                    Logger.Information("Appium server process started (PID: {ProcessId})", _appiumProcess.Id);
                    Console.WriteLine($"[INFO] Appium server process started (PID: {_appiumProcess.Id})");

                    // Wait for server to be ready
                    var timeout = TimeSpan.FromSeconds(_config.AppiumStartTimeout);
                    var startTime = DateTime.Now;
                    var isReady = false;

                    while (DateTime.Now - startTime < timeout)
                    {
                        if (IsAppiumServerRunning(serverUrl))
                        {
                            isReady = true;
                            break;
                        }
                        Thread.Sleep(1000); // Wait 1 second before checking again
                    }

                    if (isReady)
                    {
                        Logger.Information("✅ Appium server is ready at {ServerUrl}", serverUrl);
                        Console.WriteLine($"[INFO] ✅ Appium server is ready at {serverUrl}");
                        return true;
                    }
                    else
                    {
                        Logger.Warning("Appium server did not become ready within {Timeout} seconds. It may still be starting.", _config.AppiumStartTimeout);
                        Console.WriteLine($"[WARNING] Appium server did not become ready within {_config.AppiumStartTimeout} seconds. It may still be starting.");
                        // Don't throw - server might still be starting
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to start Appium server: {Error}", ex.Message);
                    Console.WriteLine($"[ERROR] Failed to start Appium server: {ex.Message}");
                    Console.WriteLine($"[ERROR] Please start Appium server manually: appium --port 4723");
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops the Appium server if it was started by this manager.
        /// </summary>
        public static void StopAppiumServer()
        {
            lock (_lock)
            {
                try
                {
                    if (_appiumProcess != null && !_appiumProcess.HasExited)
                    {
                        Logger.Information("Stopping Appium server (PID: {ProcessId})...", _appiumProcess.Id);
                        Console.WriteLine($"[INFO] Stopping Appium server (PID: {_appiumProcess.Id})...");

                        try
                        {
                            _appiumProcess.Kill();
                            _appiumProcess.WaitForExit(5000); // Wait up to 5 seconds
                            Logger.Information("✅ Appium server stopped successfully");
                            Console.WriteLine("[INFO] ✅ Appium server stopped successfully");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning("Error stopping Appium server: {Error}", ex.Message);
                            Console.WriteLine($"[WARNING] Error stopping Appium server: {ex.Message}");
                        }
                        finally
                        {
                            _appiumProcess?.Dispose();
                            _appiumProcess = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning("Failed to stop Appium server: {Error}", ex.Message);
                    Console.WriteLine($"[WARNING] Failed to stop Appium server: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if the URL points to localhost.
        /// </summary>
        private static bool IsLocalhost(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLower();
                return host == "localhost" || host == "127.0.0.1" || host == "::1" || host == "0.0.0.0";
            }
            catch
            {
                return false;
            }
        }
    }
}

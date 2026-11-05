using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA2;
using FlaUI.UIA3;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Drivers
{
    /// <summary>
    /// Central factory for Windows desktop automation sessions using FlaUI
    /// (Microsoft UI Automation). No external driver server is required.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class DesktopSessionFactory
    {
        private static DesktopSession? _session;
        private static readonly object SyncRoot = new();
        private static readonly TestConfiguration Config = TestConfiguration.Instance;

        public static DesktopSession GetSession()
        {
            if (_session != null)
            {
                return _session;
            }

            lock (SyncRoot)
            {
                if (_session != null)
                {
                    return _session;
                }

                if (!System.OperatingSystem.IsWindows())
                {
                    throw new PlatformNotSupportedException(
                        "Desktop automation with FlaUI requires Windows.");
                }

                if (string.IsNullOrWhiteSpace(Config.DesktopAppId) &&
                    string.IsNullOrWhiteSpace(Config.DesktopProcessName))
                {
                    throw new InvalidOperationException(
                        "Desktop:AppId is not configured. " +
                        "For Win32/WPF apps this should be the full path to the .exe. " +
                        "For UWP/Store apps this should be the AUMID (application user model ID). " +
                        "Alternatively set Desktop:ProcessName to attach to a running process.");
                }

                var automation = CreateAutomation();
                Application? application = null;

                try
                {
                    application = StartOrAttachApplication();
                    var window = ResolveMainWindow(application, automation);
                    _session = new DesktopSession(application, automation, window);

                    Logger.Information(
                        "FlaUI desktop session started. Process={Process} Pid={Pid} Window='{Title}' Backend={Backend}",
                        application.Name,
                        application.ProcessId,
                        window.Title,
                        automation.GetType().Name);
                    Console.WriteLine(
                        $"[INFO] FlaUI desktop session started ({application.Name}, PID {application.ProcessId}, '{window.Title}').");

                    return _session;
                }
                catch
                {
                    try { application?.Dispose(); } catch { /* ignored */ }
                    try { automation.Dispose(); } catch { /* ignored */ }
                    throw;
                }
            }
        }

        public static bool HasSession
        {
            get
            {
                lock (SyncRoot)
                {
                    return _session != null;
                }
            }
        }

        public static void Quit()
        {
            lock (SyncRoot)
            {
                try
                {
                    _session?.Dispose();
                }
                finally
                {
                    _session = null;
                }
            }
        }

        private static AutomationBase CreateAutomation()
        {
            var backend = Config.DesktopAutomationType?.Trim() ?? "UIA3";
            if (backend.Equals("UIA2", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Information("Using FlaUI UIA2 backend (recommended for some WinForms applications).");
                return new UIA2Automation();
            }

            Logger.Information("Using FlaUI UIA3 backend (recommended for WPF, WinUI, and Store apps).");
            return new UIA3Automation();
        }

        private static Application StartOrAttachApplication()
        {
            var appId = Config.DesktopAppId?.Trim() ?? string.Empty;
            var arguments = Config.DesktopArguments ?? string.Empty;
            var mode = (Config.DesktopLaunchMode ?? "Auto").Trim();
            var looksLikeAumid = appId.Contains('!', StringComparison.Ordinal);

            if (mode.Equals("Attach", StringComparison.OrdinalIgnoreCase))
            {
                return AttachToProcess(requireExisting: true);
            }

            if (mode.Equals("StoreApp", StringComparison.OrdinalIgnoreCase) ||
                (mode.Equals("Auto", StringComparison.OrdinalIgnoreCase) && looksLikeAumid))
            {
                Logger.Information("Launching Store/UWP app via AUMID '{AppId}'.", appId);
                Console.WriteLine($"[INFO] Launching Store/UWP app '{appId}' via FlaUI.");
                return Application.LaunchStoreApp(appId, arguments);
            }

            if (mode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                var attached = TryAttachToProcess();
                if (attached != null)
                {
                    Logger.Information("Attached to existing process '{Process}'.", attached.Name);
                    Console.WriteLine($"[INFO] Attached to existing desktop process '{attached.Name}' (PID {attached.ProcessId}).");
                    return attached;
                }
            }

            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new InvalidOperationException(
                    "Cannot launch a desktop application because Desktop:AppId is empty and no matching process was found to attach.");
            }

            return LaunchExecutable(appId, arguments);
        }

        private static Application LaunchExecutable(string appId, string arguments)
        {
            Logger.Information("Launching desktop executable '{AppId}'.", appId);
            Console.WriteLine($"[INFO] Launching desktop executable '{appId}' via FlaUI.");

            var workingDirectory = Config.DesktopWorkingDirectory;
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                var startInfo = new ProcessStartInfo(appId, arguments)
                {
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                };
                return Application.Launch(startInfo);
            }

            return Application.Launch(appId, arguments);
        }

        private static Application AttachToProcess(bool requireExisting)
        {
            var attached = TryAttachToProcess();
            if (attached != null)
            {
                return attached;
            }

            if (requireExisting)
            {
                var name = string.IsNullOrWhiteSpace(Config.DesktopProcessName)
                    ? Config.DesktopAppId
                    : Config.DesktopProcessName;
                throw new InvalidOperationException(
                    $"Desktop:LaunchMode is Attach but no process named '{name}' is running.");
            }

            throw new InvalidOperationException("Unable to attach to a desktop process.");
        }

        private static Application? TryAttachToProcess()
        {
            var processName = Config.DesktopProcessName;
            if (string.IsNullOrWhiteSpace(processName) && !string.IsNullOrWhiteSpace(Config.DesktopAppId))
            {
                processName = Path.GetFileNameWithoutExtension(Config.DesktopAppId);
            }

            if (string.IsNullOrWhiteSpace(processName) || processName.Contains('!'))
            {
                return null;
            }

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                return null;
            }

            return Application.Attach(processes[0]);
        }

        private static Window ResolveMainWindow(Application application, AutomationBase automation)
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(1, Config.DesktopLaunchTimeoutSeconds));
            var windowTitle = Config.DesktopWindowTitle;

            application.WaitWhileMainHandleIsMissing(timeout);

            Window? window = null;
            try
            {
                window = application.GetMainWindow(automation, timeout);
            }
            catch (Exception ex)
            {
                Logger.Debug("GetMainWindow threw: {Error}", ex.Message);
            }

            if (IsUsableWindow(window, windowTitle))
            {
                return window!;
            }

            window = Retry.WhileNull(
                () => FindWindow(application, automation, windowTitle),
                timeout,
                TimeSpan.FromMilliseconds(250),
                throwOnTimeout: false,
                ignoreException: true).Result;

            if (window == null)
            {
                throw new TimeoutException(
                    $"FlaUI could not resolve the desktop application window within {timeout.TotalSeconds:0}s. " +
                    $"AppId='{Config.DesktopAppId}', ProcessName='{Config.DesktopProcessName}', WindowTitle='{windowTitle}'.");
            }

            return window;
        }

        private static Window? FindWindow(Application application, AutomationBase automation, string windowTitle)
        {
            var fromProcess = application.GetAllTopLevelWindows(automation);
            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                var titled = fromProcess.FirstOrDefault(w =>
                    !string.IsNullOrWhiteSpace(w.Title) &&
                    w.Title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase));
                if (titled != null)
                {
                    return titled;
                }

                var desktopMatch = automation.GetDesktop().FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.Window).And(cf.ByName(windowTitle)));
                if (desktopMatch != null)
                {
                    return desktopMatch.AsWindow();
                }
            }

            if (fromProcess.Length > 0)
            {
                return fromProcess[0];
            }

            var byPid = automation.GetDesktop()
                .FindAllChildren(cf =>
                    cf.ByControlType(ControlType.Window).And(cf.ByProcessId(application.ProcessId)));
            return byPid.Select(e => e.AsWindow()).FirstOrDefault();
        }

        private static bool IsUsableWindow(Window? window, string windowTitle)
        {
            if (window == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                return true;
            }

            try
            {
                return !string.IsNullOrWhiteSpace(window.Title) &&
                       window.Title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}

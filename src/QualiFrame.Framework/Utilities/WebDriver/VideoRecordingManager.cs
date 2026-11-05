using System;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Optional per-thread screenshot-sequence recorder. No-op unless TestConfiguration:VideoRecording is true
    /// (default false), so existing runs are unchanged.
    /// </summary>
    public static class VideoRecordingManager
    {
        private static readonly AsyncLocal<Session?> Current = new();

        public static void TryStart(IWebDriver driver)
        {
            if (!TestConfiguration.Instance.VideoRecording || driver == null)
            {
                return;
            }

            Stop();

            try
            {
                var outputDir = ResolveOutputDirectory();
                Directory.CreateDirectory(outputDir);
                var session = new Session(driver, outputDir);
                Current.Value = session;
                session.Start();
                Logger.Information("Video frame capture started: {Path}", outputDir);
            }
            catch (Exception ex)
            {
                Logger.Warning("Video recording could not start: {Error}", ex.Message);
            }
        }

        public static void Stop()
        {
            var session = Current.Value;
            if (session == null)
            {
                return;
            }

            Current.Value = null;
            try
            {
                session.Dispose();
                Logger.Information("Video frame capture stopped: {Path} ({FrameCount} frames)", session.OutputDirectory, session.FrameCount);
            }
            catch (Exception ex)
            {
                Logger.Warning("Video recording stop failed: {Error}", ex.Message);
            }
        }

        private static string ResolveOutputDirectory()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Results", "Videos");
            var folder = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Environment.CurrentManagedThreadId}";
            return Path.GetFullPath(Path.Combine(root, folder));
        }

        private sealed class Session : IDisposable
        {
            private readonly IWebDriver _driver;
            private readonly Timer _timer;
            private int _frameCount;
            private int _capturing;

            public Session(IWebDriver driver, string outputDirectory)
            {
                _driver = driver;
                OutputDirectory = outputDirectory;
                _timer = new Timer(CaptureFrame, null, Timeout.Infinite, Timeout.Infinite);
            }

            public string OutputDirectory { get; }
            public int FrameCount => _frameCount;

            public void Start()
            {
                _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }

            private void CaptureFrame(object? state)
            {
                if (Interlocked.Exchange(ref _capturing, 1) == 1)
                {
                    return;
                }

                try
                {
                    if (_driver is not ITakesScreenshot screenshotDriver)
                    {
                        return;
                    }

                    var frame = screenshotDriver.GetScreenshot();
                    var index = Interlocked.Increment(ref _frameCount);
                    var path = Path.Combine(OutputDirectory, $"frame_{index:D5}.png");
                    frame.SaveAsFile(path);
                }
                catch
                {
                    // Driver may be navigating or already closed; skip the frame.
                }
                finally
                {
                    Interlocked.Exchange(ref _capturing, 0);
                }
            }

            public void Dispose()
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
            }
        }
    }
}

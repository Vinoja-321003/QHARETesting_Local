using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using QualiFrame.Framework.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.File;

namespace QualiFrame.Framework.Utilities.Logging
{
    /// <summary>
    /// Centralized logging manager using Serilog.
    /// </summary>
    public static class Logger
    {
        private static ILogger? _logger;
        private static readonly object _lock = new object();
        private static string? _correlationId;
        private static string? _testId;
        private static string? _browser;
        private static string? _environment;

        public static string? CorrelationId
        {
            get => _correlationId;
            set
            {
                _correlationId = value;
                LogContext.PushProperty("CorrelationId", value);
            }
        }

        public static string? TestId
        {
            get => _testId;
            set
            {
                _testId = value;
                LogContext.PushProperty("TestId", value);
            }
        }

        public static string? Browser
        {
            get => _browser;
            set
            {
                _browser = value;
                LogContext.PushProperty("Browser", value);
            }
        }

        public static string? Environment
        {
            get => _environment;
            set
            {
                _environment = value;
                LogContext.PushProperty("Environment", value);
            }
        }

        public static ILogger Instance
        {
            get
            {
                if (_logger == null)
                {
                    lock (_lock)
                    {
                        if (_logger == null)
                        {
                            Initialize();
                        }
                    }
                }
                return _logger!;
            }
        }

        public static void Initialize()
        {
            var config = TestConfiguration.Instance;
            var logLevel = Enum.Parse<LogEventLevel>(config.LogLevel, true);
            var logPath = config.LogPath;
            var logFileName = config.LogFileName.Replace("{Date}", DateTime.Now.ToString("yyyy-MM-dd"));

            Directory.CreateDirectory(logPath);

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "QualiFrame")
                .Enrich.WithProperty("Version", "3.0.0")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] [{TestId}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logPath, logFileName),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: config.LogMaxFiles,
                    fileSizeLimitBytes: ParseFileSize(config.LogMaxFileSize),
                    rollOnFileSizeLimit: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] [{TestId}] [{Browser}] [{Environment}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: Path.Combine(logPath, logFileName.Replace(".log", ".json")),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: config.LogMaxFiles,
                    fileSizeLimitBytes: ParseFileSize(config.LogMaxFileSize),
                    rollOnFileSizeLimit: true);

            _logger = loggerConfiguration.CreateLogger();
            Log.Logger = _logger;
        }

        public static void SetCorrelationId(string? correlationId = null)
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }

        public static void SetTestId(string testId)
        {
            TestId = testId;
        }

        public static void SetBrowser(string browser)
        {
            Browser = browser;
        }

        public static void SetEnvironment(string environment)
        {
            Environment = environment;
        }

        public static void Debug(string message, params object[] args)
        {
            Instance.Debug(SensitiveDataMasker.Mask(message), args);
        }

        public static void Information(string message, params object[] args)
        {
            Instance.Information(SensitiveDataMasker.Mask(message), args);
        }

        public static void Warning(string message, params object[] args)
        {
            Instance.Warning(SensitiveDataMasker.Mask(message), args);
        }

        public static void Error(string message, params object[] args)
        {
            Instance.Error(SensitiveDataMasker.Mask(message), args);
        }

        public static void Error(Exception exception, string message, params object[] args)
        {
            Instance.Error(exception, SensitiveDataMasker.Mask(message), args);
        }

        public static void Fatal(string message, params object[] args)
        {
            Instance.Fatal(SensitiveDataMasker.Mask(message), args);
        }

        public static void Fatal(Exception exception, string message, params object[] args)
        {
            Instance.Fatal(exception, SensitiveDataMasker.Mask(message), args);
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }

        private static long ParseFileSize(string size)
        {
            if (string.IsNullOrEmpty(size))
                return 10 * 1024 * 1024;

            size = size.Trim().ToUpper();
            var multiplier = 1L;

            if (size.EndsWith("KB"))
            {
                multiplier = 1024;
                size = size.Substring(0, size.Length - 2);
            }
            else if (size.EndsWith("MB"))
            {
                multiplier = 1024 * 1024;
                size = size.Substring(0, size.Length - 2);
            }
            else if (size.EndsWith("GB"))
            {
                multiplier = 1024 * 1024 * 1024;
                size = size.Substring(0, size.Length - 2);
            }

            if (long.TryParse(size, out var value))
            {
                return value * multiplier;
            }

            return 10 * 1024 * 1024;
        }
    }
}

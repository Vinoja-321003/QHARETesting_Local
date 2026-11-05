using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Performance
{
    /// <summary>
    /// Tracks system resource utilization (CPU, Memory, Network).
    /// Uses platform-specific implementations when available, falls back to Process-based metrics for cross-platform support.
    /// </summary>
    public class ResourceUtilizationTracker : IDisposable
    {
        // PerformanceCounter is Windows-only and requires System.Diagnostics.PerformanceCounter NuGet package
        // These fields are conditionally used on Windows platforms when the package is available
        // For cross-platform compatibility, we use Process-based metrics as fallback
#pragma warning disable CS0649 // Field is never assigned to - intentionally unused until PerformanceCounter package is added
        private readonly IDisposable? _cpuCounter;
        private readonly IDisposable? _memoryCounter;
#pragma warning restore CS0649
        private readonly bool _usePerformanceCounters;
        private readonly List<ResourceSnapshot> _snapshots;
        private readonly object _lock = new object();
        private bool _disposed = false;
        private DateTime _startTime;

        public ResourceUtilizationTracker()
        {
            _snapshots = new List<ResourceSnapshot>();
            _startTime = DateTime.UtcNow;
            _usePerformanceCounters = false;

            try
            {
                if (_usePerformanceCounters && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Logger.Debug("Resource utilization tracker initialized (PerformanceCounter mode - Windows only)");
                }
                else
                {
                    Logger.Debug("Resource utilization tracker initialized (Process-based metrics - cross-platform)");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to initialize resource tracker: {Error}. Using Process-based metrics.", ex.Message);
                _usePerformanceCounters = false;
            }
        }

        /// <summary>
        /// Takes a snapshot of current resource utilization.
        /// </summary>
        public ResourceSnapshot TakeSnapshot(string? label = null)
        {
            lock (_lock)
            {
                var snapshot = new ResourceSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Label = label ?? $"Snapshot_{_snapshots.Count + 1}",
                    CpuUsagePercent = GetCpuUsage(),
                    MemoryUsageMB = GetMemoryUsage(),
                    AvailableMemoryMB = GetAvailableMemory(),
                    NetworkStats = GetNetworkStatistics()
                };

                _snapshots.Add(snapshot);
                Logger.Debug("Resource snapshot taken: {Label}, CPU: {Cpu}%, Memory: {Memory}MB", 
                    snapshot.Label, snapshot.CpuUsagePercent, snapshot.MemoryUsageMB);

                return snapshot;
            }
        }

        /// <summary>
        /// Gets resource utilization statistics.
        /// </summary>
        public ResourceStatistics GetStatistics()
        {
            lock (_lock)
            {
                if (_snapshots.Count == 0)
                {
                    return new ResourceStatistics();
                }

                var cpuValues = _snapshots.Select(s => s.CpuUsagePercent).Where(v => v >= 0).ToList();
                var memoryValues = _snapshots.Select(s => s.MemoryUsageMB).Where(v => v >= 0).ToList();

                return new ResourceStatistics
                {
                    TotalSnapshots = _snapshots.Count,
                    Duration = DateTime.UtcNow - _startTime,
                    AverageCpuUsage = cpuValues.Any() ? cpuValues.Average() : 0,
                    MaxCpuUsage = cpuValues.Any() ? cpuValues.Max() : 0,
                    MinCpuUsage = cpuValues.Any() ? cpuValues.Min() : 0,
                    AverageMemoryUsage = memoryValues.Any() ? memoryValues.Average() : 0,
                    MaxMemoryUsage = memoryValues.Any() ? memoryValues.Max() : 0,
                    MinMemoryUsage = memoryValues.Any() ? memoryValues.Min() : 0,
                    Snapshots = _snapshots.ToList()
                };
            }
        }

        /// <summary>
        /// Clears all snapshots.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _snapshots.Clear();
                _startTime = DateTime.UtcNow;
                Logger.Information("Resource utilization snapshots cleared");
            }
        }

        private double GetCpuUsage()
        {
            try
            {
                if (_usePerformanceCounters && _cpuCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use PerformanceCounter if available
                }

                using var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpu = process.TotalProcessorTime;
                
                Thread.Sleep(100);
                
                var endTime = DateTime.UtcNow;
                var endCpu = process.TotalProcessorTime;
                var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                var totalMs = (endTime - startTime).TotalMilliseconds;
                
                if (totalMs > 0)
                {
                    var cpuPercent = (cpuUsedMs / totalMs) * 100 * Environment.ProcessorCount;
                    return Math.Round(cpuPercent, 2);
                }
                
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        private double GetMemoryUsage()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2);
            }
            catch
            {
                return -1;
            }
        }

        private double GetAvailableMemory()
        {
            try
            {
                if (_usePerformanceCounters && _memoryCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use PerformanceCounter if available
                }

                return Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2);
            }
            catch
            {
                return -1;
            }
        }

        private NetworkStatistics GetNetworkStatistics()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                long totalBytesSent = 0;
                long totalBytesReceived = 0;

                foreach (var ni in interfaces)
                {
                    var stats = ni.GetIPStatistics();
                    totalBytesSent += stats.BytesSent;
                    totalBytesReceived += stats.BytesReceived;
                }

                return new NetworkStatistics
                {
                    BytesSent = totalBytesSent,
                    BytesReceived = totalBytesReceived,
                    ActiveInterfaces = interfaces.Count
                };
            }
            catch
            {
                return new NetworkStatistics();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Resource utilization snapshot.
    /// </summary>
    public class ResourceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public string Label { get; set; } = string.Empty;
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMB { get; set; }
        public double AvailableMemoryMB { get; set; }
        public NetworkStatistics NetworkStats { get; set; } = new();
    }

    /// <summary>
    /// Network statistics.
    /// </summary>
    public class NetworkStatistics
    {
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public int ActiveInterfaces { get; set; }
    }

    /// <summary>
    /// Resource utilization statistics.
    /// </summary>
    public class ResourceStatistics
    {
        public int TotalSnapshots { get; set; }
        public TimeSpan Duration { get; set; }
        public double AverageCpuUsage { get; set; }
        public double MaxCpuUsage { get; set; }
        public double MinCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double MaxMemoryUsage { get; set; }
        public double MinMemoryUsage { get; set; }
        public List<ResourceSnapshot> Snapshots { get; set; } = new();
    }
}

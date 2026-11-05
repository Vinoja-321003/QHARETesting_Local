using System.Diagnostics;

namespace QualiFrame.Pdf.Core.Engine.Integration;

/// <summary>
/// Waits for PDF file(s) in a directory (e.g. Chrome/Edge download folder). Thread-safe.
/// </summary>
public class PdfDownloadCapture : IPdfDownloadCapture
{
    private readonly int _pollIntervalMs;
    private readonly int _maxRetries;

    public PdfDownloadCapture(int pollIntervalMs = 500, int maxRetries = 60)
    {
        _pollIntervalMs = pollIntervalMs;
        _maxRetries = maxRetries;
    }

    public async Task<string> WaitForDownloadedPdfAsync(string downloadDirectory, string? fileNamePattern = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var paths = await WaitForDownloadedPdfsAsync(downloadDirectory, fileNamePattern, expectedCount: 1, timeout, cancellationToken).ConfigureAwait(false);
        return paths.Count > 0 ? paths[0] : throw new InvalidOperationException("No PDF found in directory within timeout.");
    }

    public async Task<IReadOnlyList<string>> WaitForDownloadedPdfsAsync(string downloadDirectory, string? fileNamePattern = null, int? expectedCount = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var timeoutMs = (int)(timeout?.TotalMilliseconds ?? 30000);
        var deadline = Stopwatch.StartNew();
        var list = new List<string>();
        while (deadline.ElapsedMilliseconds < timeoutMs && !cancellationToken.IsCancellationRequested)
        {
            list.Clear();
            if (!Directory.Exists(downloadDirectory))
            {
                await Task.Delay(_pollIntervalMs, cancellationToken).ConfigureAwait(false);
                continue;
            }
            var files = Directory.GetFiles(downloadDirectory, "*.pdf", SearchOption.TopDirectoryOnly)
                .Where(f => !IsPartiallyWritten(f))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
            if (!string.IsNullOrEmpty(fileNamePattern))
            {
                var pattern = fileNamePattern.Replace("*", ".*").Replace("?", ".");
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                files = files.Where(f => regex.IsMatch(Path.GetFileName(f))).ToList();
            }
            list.AddRange(files);
            if (expectedCount.HasValue && list.Count >= expectedCount.Value)
                return list.Take(expectedCount.Value).ToList();
            if (!expectedCount.HasValue && list.Count > 0)
                return list;
            await Task.Delay(_pollIntervalMs, cancellationToken).ConfigureAwait(false);
        }
        return list;
    }

    public string StoreToArtifacts(string pdfPath, string artifactsBasePath, string? testName = null)
    {
        var name = testName ?? Path.GetFileNameWithoutExtension(pdfPath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dir = Path.Combine(artifactsBasePath, name, timestamp);
        Directory.CreateDirectory(dir);
        var destPath = Path.Combine(dir, Path.GetFileName(pdfPath));
        File.Copy(pdfPath, destPath, overwrite: true);
        return destPath;
    }

    private static bool IsPartiallyWritten(string path)
    {
        try
        {
            using var _ = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return false;
        }
        catch
        {
            return true;
        }
    }
}


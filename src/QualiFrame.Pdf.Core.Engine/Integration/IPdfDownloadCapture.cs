using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Integration;

/// <summary>
/// Captures PDFs from UI downloads (Chrome/Edge), handles dynamic filenames and parallel downloads.
/// </summary>
public interface IPdfDownloadCapture
{
    /// <summary>
    /// Wait for a single PDF to appear in the download directory (by pattern or latest file).
    /// </summary>
    Task<string> WaitForDownloadedPdfAsync(string downloadDirectory, string? fileNamePattern = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for multiple PDFs (e.g. batch) and return paths. Optional pattern and max count.
    /// </summary>
    Task<IReadOnlyList<string>> WaitForDownloadedPdfsAsync(string downloadDirectory, string? fileNamePattern = null, int? expectedCount = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy/move downloaded PDF to run artifacts folder with a stable name for reporting.
    /// </summary>
    string StoreToArtifacts(string pdfPath, string artifactsBasePath, string? testName = null);
}


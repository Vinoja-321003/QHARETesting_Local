using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Core;

/// <summary>
/// Generates a third PDF diff report that highlights differences by page and includes a summary/mismatch index.
/// </summary>
public interface IPdfDiffReportGenerator
{
    /// <summary>
    /// Generate a diff PDF from comparison result and write to output path.
    /// </summary>
    string GenerateDiffReport(
        string basePdfPath,
        string actualPdfPath,
        PdfComparisonResult comparisonResult,
        string outputDirectory,
        PdfDiffReportOptions? options = null);
}

/// <summary>
/// Options for diff report generation.
/// </summary>
public class PdfDiffReportOptions
{
    public bool IncludeSummaryPage { get; set; } = true;
    public bool IncludeMismatchIndex { get; set; } = true;
    public string? ReportTitle { get; set; }
    public bool HighlightTextDiffs { get; set; } = true;
    public bool IncludeSideBySide { get; set; }
}


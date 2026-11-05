namespace QualiFrame.Pdf.Core.Engine.Core.Contracts;

/// <summary>
/// Result of comparing base PDF vs actual/expected PDF (text, layout, or visual).
/// </summary>
public class PdfComparisonResult
{
    public bool AreEqual { get; set; }
    public PdfComparisonType ComparisonType { get; set; }
    public int TotalPagesCompared { get; set; }
    public int MismatchCount { get; set; }
    public IReadOnlyList<PdfPageComparisonDiff> PageDiffs { get; set; } = Array.Empty<PdfPageComparisonDiff>();
    public string? DiffReportPath { get; set; }
}

/// <summary>
/// Type of comparison performed.
/// </summary>
public enum PdfComparisonType
{
    Text,
    Layout,
    Visual
}

/// <summary>
/// Per-page difference details.
/// </summary>
public class PdfPageComparisonDiff
{
    public int PageNumber { get; set; }
    public bool Match { get; set; }
    public string? Summary { get; set; }
    public IReadOnlyList<TextDiffSegment>? TextDiffs { get; set; }
    public double? VisualSimilarityScore { get; set; }
}

/// <summary>
/// Segment of text diff (expected vs actual).
/// </summary>
public class TextDiffSegment
{
    public string? Expected { get; set; }
    public string? Actual { get; set; }
    public bool Equal { get; set; }
}


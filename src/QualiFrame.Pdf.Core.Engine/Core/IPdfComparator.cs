using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Core;

/// <summary>
/// Compares base PDF vs actual/expected: text, layout, and/or visual diff.
/// </summary>
public interface IPdfComparator
{
    /// <summary>
    /// Compare two PDFs by text content (with optional layout awareness).
    /// </summary>
    PdfComparisonResult CompareText(string basePdfPath, string actualPdfPath, PdfComparisonOptions? options = null);

    /// <summary>
    /// Compare two PDFs by rendered page images (pixel diff / similarity).
    /// </summary>
    PdfComparisonResult CompareVisual(string basePdfPath, string actualPdfPath, PdfComparisonOptions? options = null);

    /// <summary>
    /// Compare with layout/position awareness (coordinates, bounding boxes).
    /// </summary>
    PdfComparisonResult CompareLayout(string basePdfPath, string actualPdfPath, PdfComparisonOptions? options = null);
}

/// <summary>
/// Options for comparison: thresholds, ignore regions, masking rules.
/// </summary>
public class PdfComparisonOptions
{
    public double TextTolerance { get; set; }
    public double VisualSimilarityThreshold { get; set; } = 0.99;
    public double LayoutTolerancePts { get; set; } = 2.0;
    public IReadOnlyList<PdfIgnoreRegion>? IgnoreRegions { get; set; }
    public IReadOnlyList<PdfDynamicFieldMask>? DynamicFieldMasks { get; set; }
}

/// <summary>
/// Rule to mask dynamic fields (e.g. dates, IDs) during comparison.
/// </summary>
public class PdfDynamicFieldMask
{
    public string? RegexPattern { get; set; }
    public int? PageNumber { get; set; }
    public PdfBoundingBox? Region { get; set; }
}


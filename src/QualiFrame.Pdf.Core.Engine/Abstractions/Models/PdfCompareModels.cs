namespace QualiFrame.Pdf.Core.Engine.Abstractions.Models;

/// <summary>Options for PDF comparison (tolerances, ignore regions, masks).</summary>
public sealed class PdfCompareOptions
{
    public double TextTolerance { get; init; }
    public double VisualSimilarityThreshold { get; init; } = 0.99;
    public double LayoutTolerancePoints { get; init; } = 2.0;
    public IReadOnlyList<IgnoreRegionConfig>? IgnoreRegions { get; init; }
    public IReadOnlyList<DynamicFieldMaskConfig>? DynamicFieldMasks { get; init; }
    public bool CompareText { get; init; } = true;
    public bool CompareLayout { get; init; } = true;
    public bool CompareVisual { get; init; }
}

/// <summary>Region to ignore during comparison (e.g. headers, footers).</summary>
public sealed class IgnoreRegionConfig
{
    public int? Page { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}

/// <summary>Mask for dynamic fields (e.g. date, ID) during comparison.</summary>
public sealed class DynamicFieldMaskConfig
{
    public string? RegexPattern { get; init; }
    public int? Page { get; init; }
    public PdfBoundingBox? Region { get; init; }
}

/// <summary>Result of comparing base vs candidate PDF.</summary>
public sealed class PdfCompareResult
{
    public bool AreEqual { get; init; }
    public int TotalPagesCompared { get; init; }
    public int MismatchCount { get; init; }
    public IReadOnlyList<PageDiffInfo> PageDiffs { get; init; } = Array.Empty<PageDiffInfo>();
    public string? DiffReportPath { get; init; }
    public IReadOnlyList<string> DiffImagePaths { get; init; } = Array.Empty<string>();
}

/// <summary>Per-page diff details.</summary>
public sealed class PageDiffInfo
{
    public int PageNumber { get; init; }
    public bool Match { get; init; }
    public string? Summary { get; init; }
    public double? VisualSimilarityScore { get; init; }
    public IReadOnlyList<TextDiffSegment>? TextDiffs { get; init; }
}

/// <summary>Expected vs actual text segment.</summary>
public sealed class TextDiffSegment
{
    public string? Expected { get; init; }
    public string? Actual { get; init; }
    public bool Equal { get; init; }
}

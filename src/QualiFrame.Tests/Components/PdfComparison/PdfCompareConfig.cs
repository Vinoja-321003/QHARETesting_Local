namespace QualiFrame.Tests.Components.PdfComparison;

/// <summary>
/// Config-driven PDF comparison settings (deserialized from PdfCompareConfig.json).
/// </summary>
public class PdfCompareConfig
{
    public string BaseFilePath { get; set; } = string.Empty;
    public string CompareFilePath { get; set; } = string.Empty;
    public PdfComparisonToggles Comparisons { get; set; } = new();
}

/// <summary>
/// Toggles and tolerances for each comparison type.
/// </summary>
public class PdfComparisonToggles
{
    public bool EnableFileCompare { get; set; } = true;
    public bool EnableTextCompare { get; set; } = true;
    public bool EnableTableCompare { get; set; } = false;
    public bool EnableImageCompare { get; set; } = false;
    public bool EnableHeaderFooterCompare { get; set; } = false;
    public bool EnableFileSizeCompare { get; set; } = true;
    public bool EnableFormattingCompare { get; set; } = false;

    public long MaxSizeDifferenceBytes { get; set; } = 1024;
    public double ImageDiffTolerance { get; set; } = 0.01;
    public double LayoutTolerancePoints { get; set; } = 2.0;
}

/// <summary>
/// Result of a single comparison step for reporting.
/// </summary>
public class PdfComparisonStepResult
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Summary { get; set; } = string.Empty;
}

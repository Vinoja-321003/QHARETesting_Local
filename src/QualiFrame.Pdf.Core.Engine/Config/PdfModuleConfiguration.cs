namespace QualiFrame.Pdf.Core.Engine.Config;

/// <summary>
/// PDF module configuration (appsettings.json / QF profile). Enablement and global defaults.
/// </summary>
public class PdfModuleConfiguration
{
    public const string SectionName = "PdfAutomation";

    public bool Enabled { get; set; } = true;
    public string ArtifactsBasePath { get; set; } = "Results/artifacts/pdfs";
    public string DownloadDirectory { get; set; } = "Results/downloads";
    public int FileAvailabilityTimeoutSeconds { get; set; } = 30;
    public int FileAvailabilityPollIntervalMs { get; set; } = 500;
    public int MaxRetriesForFile { get; set; } = 3;

    /// <summary>Comparison defaults.</summary>
    public PdfComparisonProfile? Comparison { get; set; }

    /// <summary>Validation defaults.</summary>
    public PdfValidationProfile? Validation { get; set; }

    /// <summary>Performance: page-by-page streaming, cache base PDF, parallelism.</summary>
    public PdfPerformanceProfile? Performance { get; set; }
}

/// <summary>
/// Comparison thresholds and ignore/mask rules.
/// </summary>
public class PdfComparisonProfile
{
    public double VisualSimilarityThreshold { get; set; } = 0.99;
    public double LayoutTolerancePoints { get; set; } = 2.0;
    public bool IgnoreHeadersFooters { get; set; } = true;
    public IReadOnlyList<PdfIgnoreRegionConfig>? IgnoreRegions { get; set; }
    public IReadOnlyList<PdfDynamicFieldMaskConfig>? DynamicFieldMasks { get; set; }
}

/// <summary>
/// Config model for ignore region (page-relative or global).
/// </summary>
public class PdfIgnoreRegionConfig
{
    public int? Page { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

/// <summary>
/// Config model for dynamic field masking (regex or region).
/// </summary>
public class PdfDynamicFieldMaskConfig
{
    public string? Regex { get; set; }
    public int? Page { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

/// <summary>
/// Validation toggles and defaults.
/// </summary>
public class PdfValidationProfile
{
    public bool ValidateText { get; set; } = true;
    public bool ValidateTables { get; set; } = true;
    public bool ValidateFonts { get; set; } = true;
    public bool ValidateImages { get; set; } = true;
    public bool ValidateSignatures { get; set; } = false;
}

/// <summary>
/// Performance and scaling options.
/// </summary>
public class PdfPerformanceProfile
{
    public bool StreamByPage { get; set; } = true;
    public bool CacheBasePdfExtraction { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public int PageBatchSize { get; set; } = 10;
}


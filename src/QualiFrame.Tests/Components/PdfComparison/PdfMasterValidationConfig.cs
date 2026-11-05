namespace QualiFrame.Tests.Components.PdfComparison;

/// <summary>
/// Master configuration for bulk PDF validation (base vs many expected PDFs).
/// Controls which categories run and their tolerances.
/// </summary>
public class PdfMasterValidationConfig
{
    /// <summary>Relative or resolved path to the base PDF.</summary>
    public string BaseFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Relative folders (from test project root) that contain expected PDFs to validate against the base.
    /// </summary>
    public IReadOnlyList<string> ExpectedFolders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// File patterns to include within each expected folder (e.g. ["*.pdf"]).
    /// </summary>
    public IReadOnlyList<string> ExpectedFilePatterns { get; set; } = new[] { "*.pdf" };

    /// <summary>
    /// Optional hard cap on number of documents to validate (safety for large batches).
    /// </summary>
    public int? MaxDocuments { get; set; }

    /// <summary>Category toggles (comparison, text, images, tables, fonts, signatures).</summary>
    public PdfMasterCategoryToggles Categories { get; set; } = new();

    /// <summary>Per-category tolerances (layout, images, text ignore rules, etc.).</summary>
    public PdfMasterTolerances Tolerances { get; set; } = new();
}

public class PdfMasterCategoryToggles
{
    public bool EnableComparisonSummary { get; set; } = true;
    public bool EnableTextValidation { get; set; } = true;
    public bool EnableImageValidation { get; set; } = false;
    public bool EnableTableValidation { get; set; } = false;
    public bool EnableFontValidation { get; set; } = false;
    public bool EnableSignatureValidation { get; set; } = false;
}

public class PdfMasterTolerances
{
    /// <summary>Maximum allowed layout delta in points when comparing coordinates.</summary>
    public double LayoutTolerancePoints { get; set; } = 2.0;

    /// <summary>Maximum allowed visual/image difference ratio (0–1).</summary>
    public double ImageDiffTolerance { get; set; } = 0.01;

    /// <summary>Optional regex patterns to ignore in text comparisons (e.g. dates, IDs).</summary>
    public IReadOnlyList<string> TextIgnorePatterns { get; set; } = Array.Empty<string>();
}


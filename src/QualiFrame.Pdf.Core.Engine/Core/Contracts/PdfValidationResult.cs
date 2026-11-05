namespace QualiFrame.Pdf.Core.Engine.Core.Contracts;

/// <summary>
/// Unified result contract for PDF validation consumable by QualiFrame reporting (Allure/Extent).
/// </summary>
public class PdfValidationResult
{
    /// <summary>Overall pass/fail.</summary>
    public bool Passed { get; set; }

    /// <summary>Total validation checks executed.</summary>
    public int TotalChecks { get; set; }

    /// <summary>Number of failed checks.</summary>
    public int TotalFailures { get; set; }

    /// <summary>Total validation duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Individual failure details for reporting.</summary>
    public IReadOnlyList<PdfValidationFailure> Failures { get; set; } = Array.Empty<PdfValidationFailure>();

    /// <summary>Paths/links to evidence: base PDF, actual PDF, diff PDF, extracted JSON, screenshots.</summary>
    public IReadOnlyList<PdfEvidenceAttachment> Attachments { get; set; } = Array.Empty<PdfEvidenceAttachment>();
}

/// <summary>
/// Single validation failure with location and evidence reference.
/// </summary>
public class PdfValidationFailure
{
    public string RuleId { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? Location { get; set; }
    public string? Expected { get; set; }
    public string? Actual { get; set; }
    public string? EvidencePath { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Evidence artifact for reporting attachment.
/// </summary>
public class PdfEvidenceAttachment
{
    public string Name { get; set; } = string.Empty;
    public string PathOrUri { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
}


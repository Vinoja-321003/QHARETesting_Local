namespace QualiFrame.Pdf.Core.Engine.Abstractions.Models;

/// <summary>
/// Top-level report aggregating all validator outputs and summary metrics.
/// </summary>
public sealed class PdfValidationReport
{
    public string ProfileName { get; init; } = string.Empty;
    public string BasePdfPath { get; init; } = string.Empty;
    public string? CandidatePdfPath { get; init; }
    public DateTime RunUtc { get; init; }
    public long TotalDurationMs { get; init; }
    public ValidationStatus OverallStatus { get; init; }
    public int TotalFindings { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public IReadOnlyList<ValidatorResult> ValidatorResults { get; init; } = Array.Empty<ValidatorResult>();
    public IReadOnlyList<Finding> AllFindings { get; init; } = Array.Empty<Finding>();
    public IReadOnlyList<ReportAttachment> Artifacts { get; init; } = Array.Empty<ReportAttachment>();
}

/// <summary>Artifact emitted by the validation run (path and type).</summary>
public sealed class ReportAttachment
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string MimeType { get; init; } = "application/octet-stream";
}

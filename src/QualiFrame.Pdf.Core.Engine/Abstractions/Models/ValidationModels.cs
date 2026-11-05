namespace QualiFrame.Pdf.Core.Engine.Abstractions.Models;

/// <summary>Overall status of a validation or finding.</summary>
public enum ValidationStatus
{
    Pass,
    Fail,
    Warning,
    Skipped
}

/// <summary>Severity of a single finding.</summary>
public enum FindingSeverity
{
    Error,
    Warning,
    Info
}

/// <summary>
/// Result of a single validator run (one aspect). Aggregated by <see cref="PdfValidationReport"/>.
/// </summary>
public sealed class ValidatorResult
{
    public string ValidatorId { get; init; } = string.Empty;
    public string ValidatorName { get; init; } = string.Empty;
    public ValidationStatus Status { get; init; }
    public IReadOnlyList<Finding> Findings { get; init; } = Array.Empty<Finding>();
    public long DurationMs { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// A single validation finding: severity, location, expected/actual, and optional attachments.
/// </summary>
public sealed class Finding
{
    public FindingSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
    public PdfBoundingBox? BoundingBox { get; init; }
    public string? Expected { get; init; }
    public string? Actual { get; init; }
    public IReadOnlyList<FindingAttachment> Attachments { get; init; } = Array.Empty<FindingAttachment>();
}

/// <summary>Attachment for a finding (diff image, marked-up page, JSON snapshot).</summary>
public sealed class FindingAttachment
{
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string MimeType { get; init; } = "application/octet-stream";
}

/// <summary>Bounding box in PDF coordinates (points).</summary>
public sealed class PdfBoundingBox
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}

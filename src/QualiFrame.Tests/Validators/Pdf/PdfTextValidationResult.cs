namespace QualiFrame.Tests.Validators.Pdf;

/// <summary>
/// Result of a PDF text occurrence validation for reporting and assertions.
/// </summary>
public class PdfTextValidationResult
{
    public string PdfPath { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int ActualCount { get; set; }
    public bool IsSuccess { get; set; }
    public int? PageCount { get; set; }
    public long DurationMs { get; set; }
    public string? FailureMessage { get; set; }
}

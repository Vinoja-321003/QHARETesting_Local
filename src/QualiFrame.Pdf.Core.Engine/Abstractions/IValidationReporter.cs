using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Maps validation results into QualiFrame reporting (Allure/Extent) and emits artifacts (report.json, diff images, parsed-model.json).
/// </summary>
public interface IValidationReporter
{
    /// <summary>
    /// Report the full validation report to the current test report and write artifacts to the output folder.
    /// </summary>
    Task ReportAsync(
        PdfValidationReport report,
        string artifactsOutputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attach a single validator result to the current test report (e.g. step-by-step).
    /// </summary>
    void AttachValidatorResult(ValidatorResult result, string? basePdfPath = null, string? candidatePdfPath = null);
}

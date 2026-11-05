using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Reporting;

/// <summary>
/// Attaches PDF validation results and diff artifacts to QualiFrame reporting (Allure/Extent).
/// </summary>
public interface IPdfReportingBridge
{
    /// <summary>
    /// Add validation result to current test report and attach artifacts (base, actual, diff PDF, extracted JSON).
    /// </summary>
    void AttachValidationResult(PdfValidationResult result, string? basePdfPath = null, string? actualPdfPath = null, string? diffReportPath = null);
}


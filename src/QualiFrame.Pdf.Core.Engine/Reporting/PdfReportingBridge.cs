using System.Text;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Reporting;

/// <summary>
/// Attaches PDF validation results and diff artifacts to QualiFrame reporting (Allure/Extent).
/// </summary>
public class PdfReportingBridge : IPdfReportingBridge
{
    private readonly IReportingService _reporting;

    public PdfReportingBridge(IReportingService reporting)
    {
        _reporting = reporting ?? throw new ArgumentNullException(nameof(reporting));
    }

    public void AttachValidationResult(PdfValidationResult result, string? basePdfPath = null, string? actualPdfPath = null, string? diffReportPath = null)
    {
        var msg = new StringBuilder();
        msg.Append($"PDF Validation: {(result.Passed ? "Passed" : "Failed")} | Checks: {result.TotalChecks} | Failures: {result.TotalFailures} | Duration: {result.DurationMs} ms");
        _reporting.AddLog(result.Passed ? LogLevel.Info : LogLevel.Error, msg.ToString());

        foreach (var f in result.Failures)
        {
            _reporting.AddLog(LogLevel.Error,
                $"[{f.RuleId}] Page {f.PageNumber}: {f.Message} | Expected: {f.Expected} | Actual: {f.Actual}");
        }

        if (!string.IsNullOrEmpty(basePdfPath) && File.Exists(basePdfPath))
            _reporting.AddLog(LogLevel.Info, $"PDF Base: {basePdfPath}");
        if (!string.IsNullOrEmpty(actualPdfPath) && File.Exists(actualPdfPath))
            _reporting.AddLog(LogLevel.Info, $"PDF Actual: {actualPdfPath}");
        if (!string.IsNullOrEmpty(diffReportPath) && File.Exists(diffReportPath))
            _reporting.AddLog(LogLevel.Info, $"PDF Diff Report: {diffReportPath}");

        foreach (var att in result.Attachments)
        {
            if (!string.IsNullOrEmpty(att.PathOrUri))
                _reporting.AddLog(LogLevel.Info, $"Attachment [{att.Name}]: {att.PathOrUri}");
        }
    }
}


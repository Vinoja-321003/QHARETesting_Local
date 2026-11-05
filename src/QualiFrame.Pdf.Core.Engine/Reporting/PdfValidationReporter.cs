using System.Text.Json;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Reporting.Impl;

/// <summary>
/// Maps validation results into QualiFrame reporting (Allure/Extent) and emits artifacts (report.json, optional diff images, parsed-model.json).
/// </summary>
public sealed class PdfValidationReporter : IValidationReporter
{
    private readonly IReportingService _reporting;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public PdfValidationReporter(IReportingService reporting)
    {
        _reporting = reporting ?? throw new ArgumentNullException(nameof(reporting));
    }

    public async Task ReportAsync(
        PdfValidationReport report,
        string artifactsOutputPath,
        CancellationToken cancellationToken = default)
    {
        _reporting.AddLog(
            report.OverallStatus == ValidationStatus.Pass ? LogLevel.Info : LogLevel.Error,
            $"PDF Validation [{report.ProfileName}]: {report.OverallStatus} | Findings: {report.TotalFindings} (Errors: {report.ErrorCount}, Warnings: {report.WarningCount}) | Duration: {report.TotalDurationMs} ms");

        _reporting.AddLog(LogLevel.Info, $"Base PDF: {report.BasePdfPath}");
        if (!string.IsNullOrEmpty(report.CandidatePdfPath))
            _reporting.AddLog(LogLevel.Info, $"Candidate PDF: {report.CandidatePdfPath}");

        foreach (var result in report.ValidatorResults)
        {
            var level = result.Status == ValidationStatus.Fail ? LogLevel.Error
                : result.Status == ValidationStatus.Warning ? LogLevel.Warning
                : LogLevel.Info;
            _reporting.AddLog(level, $"[{result.ValidatorId}] {result.Status}: {result.Findings.Count} finding(s)");
            foreach (var f in result.Findings.Take(20))
            {
                var loc = f.PageNumber.HasValue ? $" Page {f.PageNumber}" : "";
                _reporting.AddLog(level, $"  {f.Code}{loc}: {f.Message}");
            }
            if (result.Findings.Count > 20)
                _reporting.AddLog(LogLevel.Info, $"  ... and {result.Findings.Count - 20} more.");
        }

        Directory.CreateDirectory(artifactsOutputPath);
        var artifacts = new List<ReportAttachment>();

        var reportPath = Path.Combine(artifactsOutputPath, "report.json");
        var reportDto = new ReportJsonDto
        {
            ProfileName = report.ProfileName,
            BasePdfPath = report.BasePdfPath,
            CandidatePdfPath = report.CandidatePdfPath,
            RunUtc = report.RunUtc,
            TotalDurationMs = report.TotalDurationMs,
            OverallStatus = report.OverallStatus.ToString(),
            TotalFindings = report.TotalFindings,
            ErrorCount = report.ErrorCount,
            WarningCount = report.WarningCount,
            ValidatorResults = report.ValidatorResults.Select(r => new ValidatorResultDto
            {
                ValidatorId = r.ValidatorId,
                ValidatorName = r.ValidatorName,
                Status = r.Status.ToString(),
                FindingCount = r.Findings.Count,
                DurationMs = r.DurationMs,
                Message = r.Message
            }).ToList(),
            Findings = report.AllFindings.Select(f => new FindingDto
            {
                Severity = f.Severity.ToString(),
                Code = f.Code,
                Message = f.Message,
                PageNumber = f.PageNumber,
                Expected = f.Expected,
                Actual = f.Actual
            }).ToList()
        };
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(reportDto, JsonOptions), cancellationToken).ConfigureAwait(false);
        artifacts.Add(new ReportAttachment { Name = "report.json", Path = reportPath, MimeType = "application/json" });
        _reporting.AddLog(LogLevel.Info, $"Artifact: {reportPath}");

        foreach (var att in report.Artifacts)
        {
            if (File.Exists(att.Path))
                _reporting.AddLog(LogLevel.Info, $"Artifact [{att.Name}]: {att.Path}");
        }
    }

    public void AttachValidatorResult(ValidatorResult result, string? basePdfPath = null, string? candidatePdfPath = null)
    {
        _reporting.AddLog(
            result.Status == ValidationStatus.Fail ? LogLevel.Error : LogLevel.Info,
            $"[{result.ValidatorId}] {result.Status}: {result.Message ?? string.Join("; ", result.Findings.Select(f => f.Message))}");
        if (!string.IsNullOrEmpty(basePdfPath)) _reporting.AddLog(LogLevel.Info, $"Base: {basePdfPath}");
        if (!string.IsNullOrEmpty(candidatePdfPath)) _reporting.AddLog(LogLevel.Info, $"Candidate: {candidatePdfPath}");
    }

    private sealed class ReportJsonDto
    {
        public string ProfileName { get; set; } = "";
        public string BasePdfPath { get; set; } = "";
        public string? CandidatePdfPath { get; set; }
        public DateTime RunUtc { get; set; }
        public long TotalDurationMs { get; set; }
        public string OverallStatus { get; set; } = "";
        public int TotalFindings { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<ValidatorResultDto> ValidatorResults { get; set; } = new();
        public List<FindingDto> Findings { get; set; } = new();
    }

    private sealed class ValidatorResultDto
    {
        public string ValidatorId { get; set; } = "";
        public string ValidatorName { get; set; } = "";
        public string Status { get; set; } = "";
        public int FindingCount { get; set; }
        public long DurationMs { get; set; }
        public string? Message { get; set; }
    }

    private sealed class FindingDto
    {
        public string Severity { get; set; } = "";
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public int? PageNumber { get; set; }
        public string? Expected { get; set; }
        public string? Actual { get; set; }
    }
}

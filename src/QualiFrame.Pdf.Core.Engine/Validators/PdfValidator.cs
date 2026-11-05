using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Validators;

/// <summary>
/// Orchestrates extraction and runs all requested validation rules; aggregates result for QualiFrame reporting.
/// </summary>
public class PdfValidator : IPdfValidator
{
    private readonly IPdfExtractor _extractor;

    public PdfValidator(IPdfExtractor extractor)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    }

    public PdfValidationResult Validate(string pdfPath, PdfValidationRequest request)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var extractionOptions = request.ExtractionOptions ?? new PdfExtractionOptions();
        var extracted = _extractor.Extract(pdfPath, extractionOptions);
        var failures = new List<PdfValidationFailure>();
        var totalChecks = 0;

        if (request.TextRules != null)
            foreach (var rule in request.TextRules)
            {
                totalChecks++;
                var res = rule.Validate(extracted.Pages.ToArray(), extracted);
                if (!res.Passed)
                    failures.Add(new PdfValidationFailure { RuleId = rule.RuleId, Expected = res.Expected, Actual = res.Actual, Message = res.Message });
            }

        if (request.TableRules != null)
            foreach (var rule in request.TableRules)
            {
                totalChecks++;
                var tables = extracted.Pages.SelectMany(p => p.Tables ?? Array.Empty<PdfTableInfo>()).ToList();
                var res = rule.Validate(tables, extracted);
                if (!res.Passed)
                    failures.Add(new PdfValidationFailure { RuleId = rule.RuleId, Message = res.Message });
            }

        if (request.FontRules != null)
            foreach (var rule in request.FontRules)
            {
                totalChecks++;
                var res = rule.Validate(extracted.Pages.ToArray(), extracted);
                if (!res.Passed)
                    failures.Add(new PdfValidationFailure { RuleId = rule.RuleId, PageNumber = res.Page, Location = res.Location, Message = res.Message });
            }

        if (request.ImageRules != null)
            foreach (var rule in request.ImageRules)
            {
                totalChecks++;
                var images = extracted.Pages.SelectMany(p => p.Images).ToList();
                var res = rule.Validate(images, extracted);
                if (!res.Passed)
                    failures.Add(new PdfValidationFailure { RuleId = rule.RuleId, Message = res.Message });
            }

        if (request.SignatureRules != null)
            foreach (var rule in request.SignatureRules)
            {
                totalChecks++;
                var res = rule.Validate(pdfPath, extracted);
                if (!res.Passed)
                    failures.Add(new PdfValidationFailure { RuleId = rule.RuleId, Message = res.Message });
            }

        sw.Stop();
        return new PdfValidationResult
        {
            Passed = failures.Count == 0,
            TotalChecks = totalChecks,
            TotalFailures = failures.Count,
            DurationMs = sw.ElapsedMilliseconds,
            Failures = failures
        };
    }

    public Task<PdfValidationResult> ValidateAsync(string pdfPath, PdfValidationRequest request, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Validate(pdfPath, request), cancellationToken);
    }
}


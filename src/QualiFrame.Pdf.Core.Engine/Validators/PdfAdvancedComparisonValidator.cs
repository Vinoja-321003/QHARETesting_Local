using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;
using QualiFrame.Pdf.Core.Engine.Config;

namespace QualiFrame.Pdf.Core.Engine.Validators;

/// <summary>
/// Advanced PDF comparison: text, structure, and layout/visual diff between base and candidate.
/// </summary>
public sealed class PdfAdvancedComparisonValidator : IPdfValidator
{
    private readonly Abstractions.IPdfComparer _comparer;

    public PdfAdvancedComparisonValidator(Abstractions.IPdfComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    public string Id => "Comparison";
    public string Name => "PDF Advanced Comparison";

    public async Task<ValidatorResult> ValidateAsync(PdfValidationContext request, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (string.IsNullOrEmpty(request.CandidatePdfPath))
        {
            sw.Stop();
            return new ValidatorResult
            {
                ValidatorId = Id,
                ValidatorName = Name,
                Status = ValidationStatus.Skipped,
                Message = "No candidate PDF provided for comparison.",
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        var options = request.CompareOptions;
        var opts = request.Profile.GetValidatorOptions(Id) as ComparisonOptionsConfig;
        if (opts != null)
        {
            options = new PdfCompareOptions
            {
                TextTolerance = opts.TextTolerance,
                VisualSimilarityThreshold = opts.VisualSimilarityThreshold,
                LayoutTolerancePoints = opts.LayoutTolerancePoints,
                CompareText = opts.CompareText,
                CompareLayout = opts.CompareLayout,
                CompareVisual = opts.CompareVisual
            };
        }

        var result = await _comparer.CompareAsync(request.BasePdfPath, request.CandidatePdfPath!, options, cancellationToken).ConfigureAwait(false);
        sw.Stop();

        var findings = new List<Finding>();
        foreach (var pageDiff in result.PageDiffs.Where(p => !p.Match))
        {
            findings.Add(new Finding
            {
                Severity = FindingSeverity.Error,
                Code = "COMPARISON_MISMATCH",
                Message = pageDiff.Summary ?? $"Page {pageDiff.PageNumber} mismatch.",
                PageNumber = pageDiff.PageNumber,
                Expected = pageDiff.TextDiffs?.FirstOrDefault()?.Expected,
                Actual = pageDiff.TextDiffs?.FirstOrDefault()?.Actual
            });
        }

        return new ValidatorResult
        {
            ValidatorId = Id,
            ValidatorName = Name,
            Status = result.AreEqual ? ValidationStatus.Pass : ValidationStatus.Fail,
            Findings = findings,
            DurationMs = sw.ElapsedMilliseconds,
            Message = result.AreEqual ? null : $"{result.MismatchCount} page(s) differ."
        };
    }
}

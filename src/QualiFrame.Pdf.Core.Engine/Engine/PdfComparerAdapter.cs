using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;
using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Engine;

/// <summary>
/// Adapts the existing <see cref="Core.IPdfComparator"/> to <see cref="IPdfComparer"/> for the new subsystem.
/// </summary>
public sealed class PdfComparerAdapter : IPdfComparer
{
    private readonly Core.IPdfComparator _comparator;

    public PdfComparerAdapter(Core.IPdfComparator comparator)
    {
        _comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
    }

    public async Task<PdfCompareResult> CompareAsync(
        string basePdfPath,
        string candidatePdfPath,
        PdfCompareOptions options,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var coreOptions = new PdfComparisonOptions
        {
            TextTolerance = options.TextTolerance,
            VisualSimilarityThreshold = options.VisualSimilarityThreshold,
            LayoutTolerancePts = options.LayoutTolerancePoints
        };

        PdfComparisonResult? textResult = null;
        PdfComparisonResult? layoutResult = null;
        PdfComparisonResult? visualResult = null;

        if (options.CompareText)
            textResult = _comparator.CompareText(basePdfPath, candidatePdfPath, coreOptions);
        if (options.CompareLayout)
            layoutResult = _comparator.CompareLayout(basePdfPath, candidatePdfPath, coreOptions);
        if (options.CompareVisual)
            visualResult = _comparator.CompareVisual(basePdfPath, candidatePdfPath, coreOptions);

        var primary = textResult ?? layoutResult ?? visualResult;
        if (primary == null)
            return new PdfCompareResult { AreEqual = true, TotalPagesCompared = 0 };

        var pageDiffs = primary.PageDiffs.Select(p => new PageDiffInfo
        {
            PageNumber = p.PageNumber,
            Match = p.Match,
            Summary = p.Summary,
            VisualSimilarityScore = p.VisualSimilarityScore,
            TextDiffs = p.TextDiffs?.Select(t => new Abstractions.Models.TextDiffSegment { Expected = t.Expected, Actual = t.Actual, Equal = t.Equal }).ToList()
        }).ToList();

        return new PdfCompareResult
        {
            AreEqual = primary.AreEqual,
            TotalPagesCompared = primary.TotalPagesCompared,
            MismatchCount = primary.MismatchCount,
            PageDiffs = pageDiffs,
            DiffReportPath = primary.DiffReportPath
        };
    }
}

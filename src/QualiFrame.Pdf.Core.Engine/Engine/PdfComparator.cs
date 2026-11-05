using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Engine;

/// <summary>
/// Text and layout comparison using extracted content. Visual comparison uses rendered images (see CompareVisual).
/// </summary>
public class PdfComparator : IPdfComparator
{
    private readonly IPdfExtractor _extractor;

    public PdfComparator(IPdfExtractor extractor)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    }

    public PdfComparisonResult CompareText(string basePdfPath, string actualPdfPath, PdfComparisonOptions? options = null)
    {
        options ??= new PdfComparisonOptions();
        var basePages = _extractor.ExtractTextByPage(basePdfPath);
        var actualPages = _extractor.ExtractTextByPage(actualPdfPath);
        var pageDiffs = new List<PdfPageComparisonDiff>();
        var mismatchCount = 0;
        var maxPages = Math.Max(basePages.Count, actualPages.Count);
        for (var i = 0; i < maxPages; i++)
        {
            var pageNum = i + 1;
            var baseText = i < basePages.Count ? Normalize(basePages[i], options) : string.Empty;
            var actualText = i < actualPages.Count ? Normalize(actualPages[i], options) : string.Empty;
            var match = string.Equals(baseText, actualText, StringComparison.OrdinalIgnoreCase);
            if (!match) mismatchCount++;
            pageDiffs.Add(new PdfPageComparisonDiff
            {
                PageNumber = pageNum,
                Match = match,
                Summary = match ? null : "Text mismatch",
                TextDiffs = match ? null : new[] { new TextDiffSegment { Expected = baseText, Actual = actualText, Equal = false } }
            });
        }
        return new PdfComparisonResult
        {
            AreEqual = mismatchCount == 0,
            ComparisonType = PdfComparisonType.Text,
            TotalPagesCompared = maxPages,
            MismatchCount = mismatchCount,
            PageDiffs = pageDiffs
        };
    }

    public PdfComparisonResult CompareLayout(string basePdfPath, string actualPdfPath, PdfComparisonOptions? options = null)
    {
        options ??= new PdfComparisonOptions();
        var baseResult = _extractor.Extract(basePdfPath, new PdfExtractionOptions { ExtractTables = true });
        var actualResult = _extractor.Extract(actualPdfPath, new PdfExtractionOptions { ExtractTables = true });
        var tolerance = options.LayoutTolerancePts;
        var pageDiffs = new List<PdfPageComparisonDiff>();
        var mismatchCount = 0;
        var maxPages = Math.Max(baseResult.PageCount, actualResult.PageCount);
        for (var p = 1; p <= maxPages; p++)
        {
            var basePage = baseResult.Pages.FirstOrDefault(x => x.PageNumber == p);
            var actualPage = actualResult.Pages.FirstOrDefault(x => x.PageNumber == p);
            var baseBlocks = basePage?.TextBlocks ?? Array.Empty<PdfTextBlock>();
            var actualBlocks = actualPage?.TextBlocks ?? Array.Empty<PdfTextBlock>();
            var match = baseBlocks.Count == actualBlocks.Count;
            if (match)
            {
                for (var i = 0; i < baseBlocks.Count && match; i++)
                {
                    var b = baseBlocks[i];
                    var a = actualBlocks[i];
                    match = Math.Abs(b.X - a.X) <= tolerance && Math.Abs(b.Y - a.Y) <= tolerance
                            && string.Equals(b.Text?.Trim(), a.Text?.Trim(), StringComparison.OrdinalIgnoreCase);
                }
            }
            if (!match) mismatchCount++;
            pageDiffs.Add(new PdfPageComparisonDiff
            {
                PageNumber = p,
                Match = match,
                Summary = match ? null : "Layout/text position mismatch"
            });
        }
        return new PdfComparisonResult
        {
            AreEqual = mismatchCount == 0,
            ComparisonType = PdfComparisonType.Layout,
            TotalPagesCompared = maxPages,
            MismatchCount = mismatchCount,
            PageDiffs = pageDiffs
        };
    }

    public PdfComparisonResult CompareVisual(string basePdfPath, string actualPdfPath, PdfComparisonOptions? options = null)
    {
        options ??= new PdfComparisonOptions();
        // Requires rendering pages to images and comparing (e.g. PdfPig.Rendering.Skia + image diff).
        // Placeholder: delegate to text comparison for determinism; implement pixel diff in engine when renderer is used.
        return CompareText(basePdfPath, actualPdfPath, options);
    }

    private static string Normalize(string text, PdfComparisonOptions options)
    {
        var t = text?.Trim() ?? string.Empty;
        if (options.DynamicFieldMasks != null)
            foreach (var m in options.DynamicFieldMasks)
                if (!string.IsNullOrEmpty(m.RegexPattern))
                    t = System.Text.RegularExpressions.Regex.Replace(t, m.RegexPattern, "***");
        return t;
    }
}


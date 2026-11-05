using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Engine;

/// <summary>
/// PdfPig-based implementation of extraction, comparison, and diff report generation.
/// Thread-safe when used with per-document or per-comparison instances (Transient/Scoped).
/// </summary>
public class PdfExtractor : IPdfExtractor
{
    public PdfExtractionResult Extract(string pdfPath, PdfExtractionOptions? options = null)
    {
        options ??= new PdfExtractionOptions();
        var result = new PdfExtractionResult { SourcePath = pdfPath };
        var pages = new List<PdfPageContent>();
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        result.PageCount = doc.NumberOfPages;
        var maxPages = options.MaxPages ?? result.PageCount;
        for (var i = 1; i <= Math.Min(result.PageCount, maxPages); i++)
            pages.Add(ExtractPage(pdfPath, i, options));
        result.Pages = pages;
        result.Metadata = ReadMetadata(doc);
        return result;
    }

    public PdfPageContent ExtractPage(string pdfPath, int pageNumber, PdfExtractionOptions? options = null)
    {
        options ??= new PdfExtractionOptions();
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        var page = doc.GetPage(pageNumber);
        var text = page.Text;
        var blocks = new List<PdfTextBlock>();
        foreach (var word in page.GetWords())
        {
            var g = word.BoundingBox;
            blocks.Add(new PdfTextBlock
            {
                Text = word.Text,
                X = g.Left,
                Y = g.Bottom,
                Width = g.Width,
                Height = g.Height
            });
        }
        var images = new List<PdfImageInfo>();
        if (options.ExtractImages)
        {
            var rawImages = page.GetImages();
            foreach (var img in rawImages)
            {
                var bbox = img.BoundingBox;
                images.Add(new PdfImageInfo
                {
                    PageNumber = pageNumber,
                    X = bbox.Left,
                    Y = bbox.Bottom,
                    Width = bbox.Width,
                    Height = bbox.Height
                });
            }
        }
        var tables = options.ExtractTables ? ExtractTablesForPage(doc, pageNumber) : null;
        return new PdfPageContent
        {
            PageNumber = pageNumber,
            Text = text,
            TextBlocks = blocks,
            Images = images,
            Tables = tables
        };
    }

    public int GetPageCount(string pdfPath)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        return doc.NumberOfPages;
    }

    public IReadOnlyList<string> ExtractTextByPage(string pdfPath)
    {
        var list = new List<string>();
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        for (var i = 1; i <= doc.NumberOfPages; i++)
            list.Add(doc.GetPage(i).Text);
        return list;
    }

    private static PdfDocumentMetadata? ReadMetadata(UglyToad.PdfPig.PdfDocument doc)
    {
        var info = doc.Information;
        if (info == null)
            return null;
        var created = info.GetCreatedDateTimeOffset();
        var modified = info.GetModifiedDateTimeOffset();
        return new PdfDocumentMetadata
        {
            Title = info.Title,
            Author = info.Author,
            Producer = info.Producer,
            CreationDate = created?.UtcDateTime,
            ModDate = modified?.UtcDateTime
        };
    }

    private static IReadOnlyList<PdfTableInfo>? ExtractTablesForPage(UglyToad.PdfPig.PdfDocument doc, int pageNumber)
    {
        // Placeholder: full table detection (e.g. grid/whitespace) can be implemented on top of PdfPig words/lines.
        return null;
    }
}


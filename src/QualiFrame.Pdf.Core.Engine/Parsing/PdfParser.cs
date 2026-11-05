using QualiFrame.Pdf.Core.Engine.Abstractions;
using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Parsing;

/// <summary>
/// Produces normalized PdfDocumentModel from PDF using PdfPig. Supports partial parsing (e.g. metadata-only).
/// </summary>
public sealed class PdfParser : IPdfParser
{
    public async Task<PdfDocumentModel> ParseAsync(string pdfPath, PdfParseOptions options, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        var pageCount = doc.NumberOfPages;
        var startPage = options.StartPage ?? 1;
        var maxPages = options.MaxPages ?? pageCount;
        var endPage = Math.Min(startPage + maxPages - 1, pageCount);
        var pages = new List<PdfPageModel>();

        for (var p = startPage; p <= endPage; p++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = doc.GetPage(p);
            var blocks = options.IncludeStructure ? BuildBlocks(page, options.IncludeFontInfo) : Array.Empty<PdfBlockModel>();
            var tables = options.IncludeTables ? BuildTablesForPage(doc, p) : Array.Empty<PdfTableModel>();
            var images = options.IncludeImages ? BuildImages(page, p) : Array.Empty<PdfImageModel>();
            pages.Add(new PdfPageModel
            {
                PageNumber = p,
                Blocks = blocks,
                Tables = tables,
                Images = images
            });
        }

        var metadata = options.IncludeMetadata ? ReadMetadata(doc) : null;
        return new PdfDocumentModel
        {
            SourcePath = pdfPath,
            PageCount = pageCount,
            Pages = pages,
            Metadata = metadata
        };
    }

    public int GetPageCount(string pdfPath)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
        return doc.NumberOfPages;
    }

    private static IReadOnlyList<PdfBlockModel> BuildBlocks(UglyToad.PdfPig.Content.Page page, bool includeFontInfo)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0)
            return Array.Empty<PdfBlockModel>();

        // Group words into lines by approximate Y (same line = similar bottom).
        const double lineTolerance = 2.0;
        var lines = new List<List<UglyToad.PdfPig.Content.Word>>();
        var currentLine = new List<UglyToad.PdfPig.Content.Word> { words[0] };
        var lastY = words[0].BoundingBox.Bottom;

        for (var i = 1; i < words.Count; i++)
        {
            var w = words[i];
            var y = w.BoundingBox.Bottom;
            if (Math.Abs(y - lastY) <= lineTolerance)
            {
                currentLine.Add(w);
            }
            else
            {
                lines.Add(currentLine);
                currentLine = new List<UglyToad.PdfPig.Content.Word> { w };
                lastY = y;
            }
        }
        lines.Add(currentLine);

        // One block per "paragraph" (we treat all lines as one block for simplicity; could split by vertical gap).
        var spans = new List<PdfSpanModel>();
        foreach (var line in lines)
        {
                foreach (var word in line)
                {
                    var fontName = (string?)null;
                    var fontSize = (double?)null;
                    var bold = false;
                    var italic = false;
                    var subset = false;
                    if (includeFontInfo)
                    {
                        try
                        {
                            var letters = word.Letters;
                            if (letters.Count > 0)
                            {
                                var first = letters[0];
                                fontName = first.FontName;
                                fontSize = first.PointSize;
                                var fd = first.FontDetails;
                                if (fd != null)
                                {
                                    try
                                    {
                                        bold = fd.IsBold;
                                        italic = fd.IsItalic;
                                        // IsSubset not available on FontDetails in PdfPig 0.1.x
                                    }
                                    catch { /* FontDetails API may vary by PdfPig version */ }
                                }
                            }
                        }
                        catch { /* ignore font detail errors */ }
                    }
                    var b = word.BoundingBox;
                    spans.Add(new PdfSpanModel
                {
                    Text = word.Text,
                    FontName = fontName,
                    FontSize = fontSize,
                    Bold = bold,
                    Italic = italic,
                    SubsetFont = subset,
                    Bounds = new PdfBoundingBox { X = b.Left, Y = b.Bottom, Width = b.Width, Height = b.Height }
                });
            }
        }

        var lineModels = new List<PdfLineModel>();
        foreach (var line in lines)
        {
            if (line.Count == 0) continue;
            var first = line[0].BoundingBox;
            var last = line[^1].BoundingBox;
            var lineBounds = new PdfBoundingBox
            {
                X = first.Left,
                Y = first.Bottom,
                Width = last.Right - first.Left,
                Height = first.Height
            };
                var lineSpans = line.Select(w =>
                {
                    var fn = (string?)null;
                    var fs = (double?)null;
                    var bl = false;
                    var it = false;
                    var sub = false;
                    if (includeFontInfo)
                    {
                        try
                        {
                            if (w.Letters.Count > 0)
                            {
                                var l0 = w.Letters[0];
                                fn = l0.FontName;
                                fs = l0.PointSize;
                                var fd = l0.FontDetails;
                                if (fd != null)
                                {
                                    try { bl = fd.IsBold; it = fd.IsItalic; /* IsSubset not in PdfPig 0.1.x */ }
                                    catch { /* FontDetails API may vary by PdfPig version */ }
                                }
                            }
                        }
                        catch { }
                    }
                    var bb = w.BoundingBox;
                return new PdfSpanModel
                {
                    Text = w.Text,
                    FontName = fn,
                    FontSize = fs,
                    Bold = bl,
                    Italic = it,
                    SubsetFont = sub,
                    Bounds = new PdfBoundingBox { X = bb.Left, Y = bb.Bottom, Width = bb.Width, Height = bb.Height }
                };
            }).ToList();
            lineModels.Add(new PdfLineModel { Bounds = lineBounds, Spans = lineSpans });
        }

        var blockBounds = lines.Count > 0
            ? (PdfBoundingBox?)new PdfBoundingBox
            {
                X = lines.SelectMany(l => l).Min(w => w.BoundingBox.Left),
                Y = lines.SelectMany(l => l).Min(w => w.BoundingBox.Bottom),
                Width = lines.SelectMany(l => l).Max(w => w.BoundingBox.Right) - lines.SelectMany(l => l).Min(w => w.BoundingBox.Left),
                Height = lines.SelectMany(l => l).Max(w => w.BoundingBox.Top) - lines.SelectMany(l => l).Min(w => w.BoundingBox.Bottom)
            }
            : null;
        return new[] { new PdfBlockModel { Bounds = blockBounds, Lines = lineModels } };
    }

    private static IReadOnlyList<PdfTableModel> BuildTablesForPage(UglyToad.PdfPig.PdfDocument doc, int pageNumber)
    {
        // Placeholder: full table detection can be added (e.g. grid/whitespace from words).
        return Array.Empty<PdfTableModel>();
    }

    private static IReadOnlyList<PdfImageModel> BuildImages(UglyToad.PdfPig.Content.Page page, int pageNumber)
    {
        var list = new List<PdfImageModel>();
        foreach (var img in page.GetImages())
        {
            var b = img.BoundingBox;
            list.Add(new PdfImageModel
            {
                PageNumber = pageNumber,
                Bounds = new PdfBoundingBox { X = b.Left, Y = b.Bottom, Width = b.Width, Height = b.Height }
            });
        }
        return list;
    }

    private static PdfDocumentMetadataModel? ReadMetadata(UglyToad.PdfPig.PdfDocument doc)
    {
        var info = doc.Information;
        if (info == null) return null;
        var created = info.GetCreatedDateTimeOffset();
        var modified = info.GetModifiedDateTimeOffset();
        return new PdfDocumentMetadataModel
        {
            Title = info.Title,
            Author = info.Author,
            Producer = info.Producer,
            CreationDate = created?.UtcDateTime,
            ModDate = modified?.UtcDateTime
        };
    }
}

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Tests.Validators.Pdf;

/// <summary>
/// Extracts full text from PDF using QualiFrame.Pdf (PdfPig). Thread-safe when used with per-call or scoped extractor.
/// </summary>
public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly IPdfExtractor _extractor;

    public PdfTextExtractor(IPdfExtractor extractor)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    }

    public string ExtractAllText(string pdfPath, bool normalizeWhitespace = true)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("PDF path cannot be null or empty.", nameof(pdfPath));
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF file not found.", pdfPath);

        PdfExtractionResult result;
        try
        {
            result = _extractor.Extract(pdfPath, new PdfExtractionOptions { ExtractTables = false, ExtractImages = false });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not read PDF: {pdfPath}. {ex.Message}", ex);
        }

        var sb = new StringBuilder();
        foreach (var page in result.Pages)
        {
            sb.Append(page.Text);
            sb.Append('\n');
        }

        var raw = sb.ToString();
        raw = NormalizeLineEndings(raw);
        if (normalizeWhitespace)
            raw = CollapseWhitespace(raw);
        return raw.Trim();
    }

    public int? GetPageCount(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            return null;
        try
        {
            return _extractor.GetPageCount(pdfPath);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return Regex.Replace(text, @"\s+", " ");
    }
}

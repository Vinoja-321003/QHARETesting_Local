using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Core;

/// <summary>
/// Extracts text, structure, images, and metadata from PDFs. Supports streaming/page-by-page for large docs.
/// </summary>
public interface IPdfExtractor
{
    /// <summary>
    /// Extract full document content. For large docs use stream-based overloads where supported.
    /// </summary>
    PdfExtractionResult Extract(string pdfPath, PdfExtractionOptions? options = null);

    /// <summary>
    /// Extract content for a single page (for streaming/large doc processing).
    /// </summary>
    PdfPageContent ExtractPage(string pdfPath, int pageNumber, PdfExtractionOptions? options = null);

    /// <summary>
    /// Get page count without loading full content.
    /// </summary>
    int GetPageCount(string pdfPath);

    /// <summary>
    /// Extract only text (simple string per page) for lightweight comparison.
    /// </summary>
    IReadOnlyList<string> ExtractTextByPage(string pdfPath);
}

/// <summary>
/// Options for extraction (tables, images, ignore regions, etc.).
/// </summary>
public class PdfExtractionOptions
{
    public bool ExtractTables { get; set; } = true;
    public bool ExtractImages { get; set; } = true;
    public bool ExtractFontInfo { get; set; } = true;
    public IReadOnlyList<PdfIgnoreRegion>? IgnoreRegions { get; set; }
    public int? MaxPages { get; set; }
}

/// <summary>
/// Region to ignore during extraction/comparison (e.g. headers, footers, date stamps).
/// </summary>
public class PdfIgnoreRegion
{
    public int? PageNumber { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}


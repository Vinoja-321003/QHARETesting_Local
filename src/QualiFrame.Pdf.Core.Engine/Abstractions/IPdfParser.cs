using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Produces a normalized document model (pages → blocks → lines → spans, plus tables, images, metadata).
/// Allows partial parsing when only certain validators need it (e.g. metadata-only for signature check).
/// </summary>
public interface IPdfParser
{
    /// <summary>
    /// Parse the PDF into a normalized document model. Options control depth (e.g. text-only, with structure, with images).
    /// </summary>
    Task<PdfDocumentModel> ParseAsync(string pdfPath, PdfParseOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get page count without full parse (lightweight).
    /// </summary>
    int GetPageCount(string pdfPath);
}

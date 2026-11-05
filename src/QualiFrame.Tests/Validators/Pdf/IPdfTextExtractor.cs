namespace QualiFrame.Tests.Validators.Pdf;

/// <summary>
/// Extracts full text from a PDF for validation. Uses framework PDF layer (QualiFrame.Pdf).
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts all text from the document with normalized line endings and optional whitespace collapse.
    /// </summary>
    /// <param name="pdfPath">Absolute path to the PDF file.</param>
    /// <param name="normalizeWhitespace">When true, collapse multiple spaces/newlines to single space (QF-safe).</param>
    /// <returns>Full document text.</returns>
    string ExtractAllText(string pdfPath, bool normalizeWhitespace = true);

    /// <summary>
    /// Gets the number of pages in the PDF, or null if unavailable.
    /// </summary>
    int? GetPageCount(string pdfPath);
}

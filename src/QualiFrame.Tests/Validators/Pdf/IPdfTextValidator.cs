namespace QualiFrame.Tests.Validators.Pdf;

/// <summary>
/// Validates text occurrence in PDFs. Counts exact occurrences across the entire document.
/// </summary>
public interface IPdfTextValidator
{
    /// <summary>
    /// Counts exact occurrences of the target text in the PDF (full document).
    /// </summary>
    int CountOccurrences(string pdfPath, string exactText, bool caseSensitive = true);

    /// <summary>
    /// Validates that the PDF contains the expected number of occurrences and returns a result for reporting.
    /// </summary>
    PdfTextValidationResult ValidateOccurrences(string pdfPath, string exactText, int expectedCount, bool caseSensitive = true);
}

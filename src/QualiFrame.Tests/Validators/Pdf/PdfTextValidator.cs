using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QualiFrame.Tests.Validators.Pdf;

/// <summary>
/// Validates exact text occurrence counts in PDFs. Thread-safe.
/// </summary>
public class PdfTextValidator : IPdfTextValidator
{
    private readonly IPdfTextExtractor _extractor;

    public PdfTextValidator(IPdfTextExtractor extractor)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    }

    public int CountOccurrences(string pdfPath, string exactText, bool caseSensitive = true)
    {
        if (string.IsNullOrEmpty(exactText))
            return 0;

        var fullText = _extractor.ExtractAllText(pdfPath, normalizeWhitespace: true);
        var pattern = Regex.Escape(exactText.Trim());
        var options = RegexOptions.None;
        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;
        var matches = Regex.Matches(fullText, pattern);
        return matches.Count;
    }

    public PdfTextValidationResult ValidateOccurrences(string pdfPath, string exactText, int expectedCount, bool caseSensitive = true)
    {
        var sw = Stopwatch.StartNew();
        int actualCount;
        int? pageCount = null;
        string? failureMessage = null;

        try
        {
            if (!System.IO.File.Exists(pdfPath))
            {
                sw.Stop();
                return new PdfTextValidationResult
                {
                    PdfPath = pdfPath,
                    TargetText = exactText,
                    ExpectedCount = expectedCount,
                    ActualCount = 0,
                    IsSuccess = false,
                    DurationMs = sw.ElapsedMilliseconds,
                    FailureMessage = $"PDF file not found: {pdfPath}"
                };
            }

            var fullText = _extractor.ExtractAllText(pdfPath, normalizeWhitespace: true);
            pageCount = _extractor.GetPageCount(pdfPath);

            actualCount = CountOccurrences(pdfPath, exactText, caseSensitive);
            var success = actualCount >= 1 && actualCount == expectedCount;
            if (!success)
            {
                failureMessage = $"Expected {expectedCount} occurrence(s) of \"{exactText}\", but found {actualCount}. PDF: {pdfPath}";
            }
        }
        catch (Exception ex)
        {
            failureMessage = $"Validation failed: {ex.Message}. PDF: {pdfPath}";
            actualCount = -1;
        }
        finally
        {
            sw.Stop();
        }

        return new PdfTextValidationResult
        {
            PdfPath = pdfPath,
            TargetText = exactText,
            ExpectedCount = expectedCount,
            ActualCount = actualCount,
            IsSuccess = actualCount >= 0 && actualCount == expectedCount,
            PageCount = pageCount,
            DurationMs = sw.ElapsedMilliseconds,
            FailureMessage = failureMessage
        };
    }
}

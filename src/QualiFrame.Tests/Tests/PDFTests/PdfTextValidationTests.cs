using System.Collections;
using NUnit.Framework;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Tests.Utilities;
using QualiFrame.Tests.Validators.Pdf;

namespace QualiFrame.Tests.Tests.PDFTests;

/// <summary>
/// Data-driven PDF text validation tests using TestCaseSource.
/// Validates existence and exact occurrence count of target text; all output via QF reporting.
/// </summary>
[TestFixture]
[Category("PDF")]
[Category("FileValidation")]
[Allure.NUnit.Attributes.AllureSuite("PDF Validations")]
[Allure.NUnit.Attributes.AllureFeature("Text Validation")]
public class PdfTextValidationTests : PdfValidationTestBase
{
    private const string DefaultPdfRelativePath = "Data/FileValidation/PDFTestFile.pdf";

    public static IEnumerable PdfTextValidationTestCases
    {
        get
        {
            yield return new TestCaseData("Data/FileValidation/PDFTestFile.pdf", "HEALTH AND SAFETY STANDARD", 1, true)
                .SetName("PdfText_HealthAndSafetyStandard_Expected1_CaseSensitive")
                .SetDescription("Validates that 'HEALTH AND SAFETY STANDARD' exists and occurs exactly once (case-sensitive). Update expected count if document changes.");
        }
    }

    [Test]
    [TestCaseSource(nameof(PdfTextValidationTestCases))]
    public void PdfText_ValidateOccurrences(
        string pdfRelativeOrResolvedPath,
        string targetText,
        int expectedCount,
        bool caseSensitive)
    {
        var pdfPath = IsResolvedPath(pdfRelativeOrResolvedPath)
            ? pdfRelativeOrResolvedPath
            : TestPathResolver.ResolveTestPath(pdfRelativeOrResolvedPath);

        LogStep("Load PDF", pdfPath);
        Reporting.AddLog(LogLevel.Info, $"PDF path (resolved): {pdfPath}");
        if (!System.IO.File.Exists(pdfPath))
        {
            Reporting.AddLog(LogLevel.Error, $"PDF not found: {pdfPath}");
            AssertWithAllure("Assert PDF exists", () => Assert.Fail($"PDF file not found: {pdfPath}"));
        }

        LogStep("Extract Text", "Full document");
        var text = PdfTextExtractor.ExtractAllText(pdfPath, normalizeWhitespace: true);
        AttachPdfArtifact(pdfPath, textPreview: text);

        LogStep("Count Occurrences", $"Target: \"{targetText}\", CaseSensitive: {caseSensitive}");
        var result = PdfValidator.ValidateOccurrences(pdfPath, targetText, expectedCount, caseSensitive);

        LogStep("Assert Results", result.IsSuccess ? "Pass" : result.FailureMessage ?? "Fail");
        Reporting.AddLog(result.IsSuccess ? LogLevel.Info : LogLevel.Error,
            $"Target: \"{targetText}\" | Expected: {result.ExpectedCount} | Actual: {result.ActualCount} | PDF: {result.PdfPath} | Duration: {result.DurationMs} ms");

        AssertWithAllure("Assert target text found",
            () => Assert.That(result.ActualCount, Is.GreaterThan(0),
                $"Target text \"{targetText}\" was not found in PDF. {result.FailureMessage}"));

        AssertWithAllure("Assert occurrence count matches expected",
            () => Assert.That(result.ActualCount, Is.EqualTo(result.ExpectedCount),
                $"Occurrence count mismatch. Expected: {result.ExpectedCount}, Actual: {result.ActualCount}. Target: \"{targetText}\". PDF: {result.PdfPath}. {result.FailureMessage}"));
    }

    private static bool IsResolvedPath(string path)
    {
        return System.IO.Path.IsPathRooted(path) && System.IO.File.Exists(path);
    }
}

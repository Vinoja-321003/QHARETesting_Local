using System.Collections;
using NUnit.Framework;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Tests.Utilities;
using QualiFrame.Tests.Validators.Pdf;

namespace QualiFrame.Tests.Tests.PDFTests;

/// <summary>
/// Suite template for extensible PDF validations. Establishes patterns for:
/// - Exact text (current), regex, page-scoped, "must contain all clauses", "must not contain".
/// Use shared validator and reporting; add new test regions or test classes per category.
/// </summary>
[TestFixture]
[Category("PDF")]
[Category("FileValidation")]
[Allure.NUnit.Attributes.AllureSuite("PDF Validations")]
[Allure.NUnit.Attributes.AllureFeature("Extensible PDF Validations")]
public class PdfValidationSuiteTests : PdfValidationTestBase
{
    #region Exact text validation (current pattern)

    [Test]
    [TestCaseSource(nameof(ExactTextCases))]
    public void ExactText_ValidateOccurrences(string pdfRelativePath, string targetText, int expectedCount, bool caseSensitive)
    {
        var pdfPath = TestPathResolver.ResolveTestPath(pdfRelativePath);
        LogStep("Load PDF", pdfPath);
        if (!System.IO.File.Exists(pdfPath))
            AssertWithAllure("Assert PDF exists", () => Assert.Fail($"PDF not found: {pdfPath}"));

        LogStep("Extract Text", "Full document");
        var text = PdfTextExtractor.ExtractAllText(pdfPath, normalizeWhitespace: true);
        AttachPdfArtifact(pdfPath, textPreview: text);

        LogStep("Count Occurrences", $"\"{targetText}\"");
        var result = PdfValidator.ValidateOccurrences(pdfPath, targetText, expectedCount, caseSensitive);
        LogStep("Assert Results", result.IsSuccess ? "Pass" : result.FailureMessage ?? "Fail");
        Reporting.AddLog(result.IsSuccess ? LogLevel.Info : LogLevel.Error,
            $"Target: \"{targetText}\" | Expected: {result.ExpectedCount} | Actual: {result.ActualCount}");

        AssertWithAllure("Assert target text found",
            () => Assert.That(result.ActualCount, Is.GreaterThan(0), result.FailureMessage));

        AssertWithAllure("Assert occurrence count matches expected",
            () => Assert.That(result.ActualCount, Is.EqualTo(expectedCount), result.FailureMessage));
    }

    private static IEnumerable ExactTextCases
    {
        get
        {
            yield return new TestCaseData("Data/FileValidation/PDFTestFile.pdf", "HEALTH AND SAFETY STANDARD", 1, true)
                .SetName("ExactText_HealthAndSafetyStandard_Count1");
        }
    }

    #endregion
}

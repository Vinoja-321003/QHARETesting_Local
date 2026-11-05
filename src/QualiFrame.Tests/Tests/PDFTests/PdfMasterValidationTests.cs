using System.Text.Json;
using NUnit.Framework;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;
using QualiFrame.Pdf.Core.Engine.Reporting;
using QualiFrame.Tests.Components.PdfComparison;
using QualiFrame.Tests.Utilities;

namespace QualiFrame.Tests.Tests.PDFTests;

/// <summary>
/// Master/batch PDF validation suite: validates many expected PDFs against a single base PDF
/// using the unified QualiFrame PDF engine and config-driven inputs.
/// </summary>
[TestFixture]
[Category("PDF")]
[Category("FileValidation")]
[Allure.NUnit.Attributes.AllureSuite("PDF Master Validation")]
[Allure.NUnit.Attributes.AllureFeature("Base vs Expected Batch Comparison")]
public class PdfMasterValidationTests : PdfValidationTestBase
{
    private static string ResolveConfigPath()
        => TestPathResolver.ResolveTestPath("Data/FileValidation/PdfMasterValidationConfig.json");

    [Test]
    [Allure.NUnit.Attributes.AllureStory("Validate multiple PDFs against base using configuration")]
    [Allure.NUnit.Attributes.AllureDescription("Reads PdfMasterValidationConfig.json and validates a batch of PDFs against a single base PDF, producing comparison summaries and diff artifacts.")]
    public void Pdf_BatchValidation_FromConfig()
    {
        LogStep("Load Master Config", "PdfMasterValidationConfig.json");
        var configPath = ResolveConfigPath();
        if (!File.Exists(configPath))
        {
            Reporting.AddLog(LogLevel.Error, $"PdfMasterValidationConfig.json not found at {configPath}");
            AssertWithAllure("Assert master config exists",
                () => Assert.Fail($"PdfMasterValidationConfig.json not found at {configPath}"));
        }

        var json = File.ReadAllText(configPath);
        var cfg = JsonSerializer.Deserialize<PdfMasterValidationConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize PdfMasterValidationConfig.json");

        var basePath = ResolvePath(cfg.BaseFilePath);
        LogStep("Resolve Base PDF", basePath);
        if (!File.Exists(basePath))
        {
            Reporting.AddLog(LogLevel.Error, $"Base PDF does not exist: {basePath}");
            AssertWithAllure("Assert base PDF exists",
                () => Assert.Fail($"Base PDF does not exist: {basePath}"));
        }

        var expectedFiles = ResolveExpectedFiles(cfg, basePath).ToList();
        if (expectedFiles.Count == 0)
        {
            Reporting.AddLog(LogLevel.Error, "No expected PDFs found for batch validation.");
            AssertWithAllure("Assert expected PDFs exist",
                () => Assert.Fail("No expected PDFs found for batch validation. Check PdfMasterValidationConfig.json."));
        }

        var maxDocs = cfg.MaxDocuments ?? expectedFiles.Count;
        if (expectedFiles.Count > maxDocs)
            expectedFiles = expectedFiles.Take(maxDocs).ToList();

        Reporting.AddLog(LogLevel.Info, $"PDF Master Validation: Base={basePath}, ExpectedCount={expectedFiles.Count}");

        var runDir = Path.Combine(
            TestPathResolver.GetProjectRoot(),
            "src", "QualiFrame.Tests", "TestResults", "PdfMasterValidation",
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(runDir);

        var comparisonResults = new List<(string expectedPath, PdfComparisonResult result, string diffPath)>();
        var extractor = (ServiceProvider.GetService(typeof(IPdfExtractor)) as IPdfExtractor)
            ?? throw new InvalidOperationException("IPdfExtractor not resolved");
        var comparator = (ServiceProvider.GetService(typeof(IPdfComparator)) as IPdfComparator)
            ?? throw new InvalidOperationException("IPdfComparator not resolved");
        var diffGenerator = (ServiceProvider.GetService(typeof(IPdfDiffReportGenerator)) as IPdfDiffReportGenerator)
            ?? throw new InvalidOperationException("IPdfDiffReportGenerator not resolved");
        var reportingBridge = (ServiceProvider.GetService(typeof(IPdfReportingBridge)) as IPdfReportingBridge);

        var options = new PdfComparisonOptions
        {
            LayoutTolerancePts = cfg.Tolerances.LayoutTolerancePoints
        };

        foreach (var expectedPath in expectedFiles)
        {
            LogStep("Compare PDFs", $"Base={Path.GetFileName(basePath)}, Expected={Path.GetFileName(expectedPath)}");

            if (!File.Exists(expectedPath))
            {
                Reporting.AddLog(LogLevel.Error, $"Expected PDF does not exist: {expectedPath}");
                AssertWithAllure("Assert expected PDF exists",
                    () => Assert.Fail($"Expected PDF does not exist: {expectedPath}"));
                continue;
            }

            var comparison = comparator.CompareText(basePath, expectedPath, options);

            var diffDir = Path.Combine(runDir, "DiffReports");
            Directory.CreateDirectory(diffDir);
            var diffPath = diffGenerator.GenerateDiffReport(basePath, expectedPath, comparison, diffDir, null);

            Reporting.AddLog(
                comparison.AreEqual ? LogLevel.Info : LogLevel.Error,
                $"[BatchCompare] Base={Path.GetFileName(basePath)}, Expected={Path.GetFileName(expectedPath)}, Equal={comparison.AreEqual}, Mismatches={comparison.MismatchCount}");

            foreach (var diff in comparison.PageDiffs.Where(d => !d.Match).Take(10))
            {
                Reporting.AddLog(LogLevel.Error,
                    $"[BatchCompare] File={Path.GetFileName(expectedPath)}, Page {diff.PageNumber}: {diff.Summary}");
            }

            comparisonResults.Add((expectedPath, comparison, diffPath));

            // Optionally attach comparison summary via reporting bridge (if available)
            if (reportingBridge != null)
            {
                var validation = new PdfValidationResult
                {
                    Passed = comparison.AreEqual,
                    TotalChecks = comparison.TotalPagesCompared,
                    TotalFailures = comparison.MismatchCount
                };
                reportingBridge.AttachValidationResult(validation, basePath, expectedPath, diffPath);
            }
        }

        Reporting.AddLog(LogLevel.Info, $"Artifacts written under: {runDir}");

        // Final assertion: all comparisons must pass when comparison summary is enabled.
        if (cfg.Categories.EnableComparisonSummary)
        {
            var failed = comparisonResults.Where(r => !r.result.AreEqual).ToList();
            AssertWithAllure("Assert all batch PDF comparisons passed", () =>
            {
                if (failed.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("One or more PDF comparisons failed.");
                    foreach (var f in failed)
                    {
                        sb.AppendLine(
                            $"- {Path.GetFileName(f.expectedPath)}: Equal={f.result.AreEqual}, Mismatches={f.result.MismatchCount}, Diff={f.diffPath}");
                    }
                    Assert.Fail(sb.ToString());
                }
            });
        }
    }

    private static string ResolvePath(string relativeOrAbsolutePath)
    {
        if (Path.IsPathRooted(relativeOrAbsolutePath) && File.Exists(relativeOrAbsolutePath))
            return relativeOrAbsolutePath;

        return TestPathResolver.ResolveTestPath(relativeOrAbsolutePath);
    }

    private static IEnumerable<string> ResolveExpectedFiles(PdfMasterValidationConfig cfg, string basePath)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in cfg.ExpectedFolders)
        {
            var folderPath = TestPathResolver.ResolveTestPath(folder);
            if (!Directory.Exists(folderPath))
                continue;

            foreach (var pattern in cfg.ExpectedFilePatterns)
            {
                var files = Directory.GetFiles(folderPath, pattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    // Exclude the base PDF itself if it happens to reside in the same folder.
                    if (!string.Equals(Path.GetFullPath(file), Path.GetFullPath(basePath), StringComparison.OrdinalIgnoreCase))
                        result.Add(file);
                }
            }
        }
        return result;
    }
}


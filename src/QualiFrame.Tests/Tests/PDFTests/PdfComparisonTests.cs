using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using QualiFrame.Framework.Core.Services;
using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Tests.Components.PdfComparison;
using QualiFrame.Tests.Utilities;
using QualiFrame.Tests.Validators.Pdf;

namespace QualiFrame.Tests.Tests.PDFTests;

/// <summary>
/// Config-driven PDF comparison test: BasePDF.pdf vs ComparePDF.pdf.
/// Reads Data/FileValidation/PdfCompareConfig.json; runs only enabled comparisons and produces artifacts.
/// </summary>
[TestFixture]
[Category("PDF")]
[Category("FileValidation")]
[Allure.NUnit.Attributes.AllureSuite("PDF Comparisons")]
[Allure.NUnit.Attributes.AllureFeature("Config-driven PDF Comparison")]
public class PdfComparisonTests : PdfValidationTestBase
{
    private static string ResolveConfigPath()
        => TestPathResolver.ResolveTestPath("Data/FileValidation/PdfCompareConfig.json");

    [Test]
    [Allure.NUnit.Attributes.AllureStory("Compare PDFs using Configuration")]
    [Allure.NUnit.Attributes.AllureDescription("Reads PdfCompareConfig.json and compares BasePDF vs ComparePDF, validating text, pages, and size.")]
    public void PdfCompare_ConfigDrivenChecks()
    {
        LogStep("Load Config", "PdfCompareConfig.json");
        var configPath = ResolveConfigPath();
        if (!File.Exists(configPath))
        {
            Reporting.AddLog(LogLevel.Error, $"PdfCompareConfig.json not found at {configPath}");
            AssertWithAllure("Assert config file exists",
                () => Assert.Fail($"PdfCompareConfig.json not found at {configPath}"));
        }

        var json = File.ReadAllText(configPath);
        var cfg = JsonSerializer.Deserialize<PdfCompareConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize PdfCompareConfig.json");

        var basePath = TestPathResolver.ResolveTestPath(cfg.BaseFilePath);
        var comparePath = TestPathResolver.ResolveTestPath(cfg.CompareFilePath);

        LogStep("Resolve PDFs", $"Base={basePath}, Compare={comparePath}");
        if (!File.Exists(basePath) || !File.Exists(comparePath))
        {
            Reporting.AddLog(LogLevel.Error, $"Base or compare PDF does not exist. Base={basePath}, Compare={comparePath}");
            AssertWithAllure("Assert base and compare PDFs exist",
                () => Assert.Fail($"Base or compare PDF does not exist. Base={basePath}, Compare={comparePath}"));
        }

        var runDir = Path.Combine(
            TestPathResolver.GetProjectRoot(),
            "src", "QualiFrame.Tests", "TestResults", "PdfCompare",
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(runDir);

        var stepResults = new List<PdfComparisonStepResult>();

        // 1) File-level: page count + optional checksum
        if (cfg.Comparisons.EnableFileCompare)
        {
            LogStep("FileCompare", "Page count + optional checksum");
            var extractor = (ServiceProvider.GetService(typeof(IPdfExtractor)) as IPdfExtractor)
                ?? throw new InvalidOperationException("IPdfExtractor not resolved");

            var basePages = extractor.GetPageCount(basePath);
            var comparePages = extractor.GetPageCount(comparePath);

            var passed = basePages == comparePages;
            var summary = $"PageCount: Base={basePages}, Compare={comparePages}";

            var baseHash = ComputeSha256(basePath);
            var compareHash = ComputeSha256(comparePath);
            var checksumInfo = $"Checksums (SHA256): Base={baseHash}, Compare={compareHash}";

            Reporting.AddLog(passed ? LogLevel.Info : LogLevel.Error, $"[FileCompare] {summary}. {checksumInfo}");
            stepResults.Add(new PdfComparisonStepResult
            {
                Name = "FileCompare",
                Passed = passed,
                Summary = $"{summary}; {checksumInfo}"
            });
        }
        else
        {
            Reporting.AddLog(LogLevel.Info, "[FileCompare] Skipped via configuration.");
        }

        // 2) File size compare
        if (cfg.Comparisons.EnableFileSizeCompare)
        {
            LogStep("FileSizeCompare", $"Tolerance={cfg.Comparisons.MaxSizeDifferenceBytes} bytes");
            var baseSize = new FileInfo(basePath).Length;
            var compareSize = new FileInfo(comparePath).Length;
            var diff = Math.Abs(baseSize - compareSize);
            var passed = diff <= cfg.Comparisons.MaxSizeDifferenceBytes;

            var summary = $"Size: Base={baseSize} bytes, Compare={compareSize} bytes, Diff={diff} bytes";
            Reporting.AddLog(passed ? LogLevel.Info : LogLevel.Error, $"[FileSizeCompare] {summary}");
            stepResults.Add(new PdfComparisonStepResult
            {
                Name = "FileSizeCompare",
                Passed = passed,
                Summary = summary
            });
        }
        else
        {
            Reporting.AddLog(LogLevel.Info, "[FileSizeCompare] Skipped via configuration.");
        }

        // 3) Text compare using framework comparator
        if (cfg.Comparisons.EnableTextCompare)
        {
            LogStep("TextCompare", "Per-page + doc-level");
            var comparator = (ServiceProvider.GetService(typeof(IPdfComparator)) as IPdfComparator)
                ?? throw new InvalidOperationException("IPdfComparator not resolved");

            var options = new PdfComparisonOptions
            {
                LayoutTolerancePts = cfg.Comparisons.LayoutTolerancePoints
            };

            var comparison = comparator.CompareText(basePath, comparePath, options);

            var textDir = Path.Combine(runDir, "text");
            Directory.CreateDirectory(textDir);
            var baseTextPath = Path.Combine(textDir, "base_text.txt");
            var compareTextPath = Path.Combine(textDir, "compare_text.txt");

            File.WriteAllText(baseTextPath, PdfTextExtractor.ExtractAllText(basePath, normalizeWhitespace: true));
            File.WriteAllText(compareTextPath, PdfTextExtractor.ExtractAllText(comparePath, normalizeWhitespace: true));

            Reporting.AddLog(
                comparison.AreEqual ? LogLevel.Info : LogLevel.Error,
                $"[TextCompare] Equal={comparison.AreEqual}, Mismatches={comparison.MismatchCount}");

            foreach (var diff in comparison.PageDiffs.Where(d => !d.Match).Take(5))
            {
                Reporting.AddLog(LogLevel.Error, $"[TextCompare] Page {diff.PageNumber}: {diff.Summary}");
            }

            stepResults.Add(new PdfComparisonStepResult
            {
                Name = "TextCompare",
                Passed = comparison.AreEqual,
                Summary = $"Equal={comparison.AreEqual}, Mismatches={comparison.MismatchCount}"
            });
        }
        else
        {
            Reporting.AddLog(LogLevel.Info, "[TextCompare] Skipped via configuration.");
        }

        if (cfg.Comparisons.EnableTableCompare)
            Reporting.AddLog(LogLevel.Info, "[TableCompare] Enabled in config but not yet implemented. Skipping.");
        if (cfg.Comparisons.EnableImageCompare)
            Reporting.AddLog(LogLevel.Info, "[ImageCompare] Enabled in config but not yet implemented. Skipping.");
        if (cfg.Comparisons.EnableHeaderFooterCompare)
            Reporting.AddLog(LogLevel.Info, "[HeaderFooterCompare] Enabled in config but not yet implemented. Skipping.");
        if (cfg.Comparisons.EnableFormattingCompare)
            Reporting.AddLog(LogLevel.Info, "[FormattingCompare] Enabled in config but not yet implemented. Skipping.");

        AttachPdfArtifact(basePath, null);
        AttachPdfArtifact(comparePath, null);
        Reporting.AddLog(LogLevel.Info, $"Artifacts written under: {runDir}");

        var failed = stepResults.Where(s => !s.Passed).ToList();
        if (failed.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PDF comparison failed. Summary:");
            foreach (var s in stepResults)
                sb.AppendLine($"- {s.Name}: {(s.Passed ? "PASS" : "FAIL")} - {s.Summary}");
            AssertWithAllure("Assert all comparison steps passed",
                () => Assert.Fail(sb.ToString()));
        }
        else
        {
            Reporting.AddLog(LogLevel.Info, "All enabled PDF comparisons passed.");
        }
    }

    private static string ComputeSha256(string path)
    {
        using var fs = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(fs);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

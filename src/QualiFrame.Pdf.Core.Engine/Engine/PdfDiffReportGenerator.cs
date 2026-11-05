using QualiFrame.Pdf.Core.Engine.Core;
using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Engine;

/// <summary>
/// Generates a diff PDF (summary + per-page highlights). Uses PdfPig for reading; output is text/HTML for now (full PDF via QuestPDF or similar if added).
/// </summary>
public class PdfDiffReportGenerator : IPdfDiffReportGenerator
{
    public string GenerateDiffReport(
        string basePdfPath,
        string actualPdfPath,
        PdfComparisonResult comparisonResult,
        string outputDirectory,
        PdfDiffReportOptions? options = null)
    {
        options ??= new PdfDiffReportOptions();
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"diff_{Path.GetFileNameWithoutExtension(actualPdfPath)}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var outputPath = Path.Combine(outputDirectory, fileName);
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("PDF Comparison Diff Report");
        summary.AppendLine($"Base: {basePdfPath}");
        summary.AppendLine($"Actual: {actualPdfPath}");
        summary.AppendLine($"Equal: {comparisonResult.AreEqual}");
        summary.AppendLine($"Mismatches: {comparisonResult.MismatchCount} of {comparisonResult.TotalPagesCompared} pages");
        foreach (var d in comparisonResult.PageDiffs.Where(x => !x.Match))
            summary.AppendLine($"  Page {d.PageNumber}: {d.Summary}");
        var reportPath = Path.Combine(outputDirectory, Path.ChangeExtension(fileName, ".txt"));
        File.WriteAllText(reportPath, summary.ToString());
        return reportPath;
    }
}


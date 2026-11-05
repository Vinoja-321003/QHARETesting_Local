using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Optional comparer: produces diffs between a base PDF and a candidate PDF (text, structure, layout/visual).
/// Used by comparison validators; can be injected separately for diff-only workflows.
/// </summary>
public interface IPdfComparer
{
    /// <summary>
    /// Compare base vs candidate and return a structured diff (text, layout, visual).
    /// </summary>
    Task<PdfCompareResult> CompareAsync(
        string basePdfPath,
        string candidatePdfPath,
        PdfCompareOptions options,
        CancellationToken cancellationToken = default);
}

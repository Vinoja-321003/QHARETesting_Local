using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Abstractions.Models;

/// <summary>
/// Context passed to each validator: base PDF, optional candidate, parsed models, and profile options.
/// Immutable; no static shared state.
/// </summary>
public sealed class PdfValidationContext
{
    public string BasePdfPath { get; init; } = string.Empty;
    public string? CandidatePdfPath { get; init; }
    public PdfDocumentModel? BaseModel { get; init; }
    public PdfDocumentModel? CandidateModel { get; init; }
    public IValidationProfile Profile { get; init; } = null!;
    public PdfParseOptions ParseOptions { get; init; } = new();
    public PdfCompareOptions CompareOptions { get; init; } = new();
}

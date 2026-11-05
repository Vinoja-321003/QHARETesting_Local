using QualiFrame.Pdf.Core.Engine.Abstractions.Models;

namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Single-responsibility validator: validates one aspect of a PDF (e.g. fonts, structure, signatures).
/// Implementations are discoverable via <see cref="IPdfValidatorRegistry"/> and run by <see cref="Engine.PdfValidationEngine"/>.
/// </summary>
public interface IPdfValidator
{
    /// <summary>Unique identifier for this validator (e.g. "Font", "Structure", "Signature").</summary>
    string Id { get; }

    /// <summary>Human-readable name for reporting.</summary>
    string Name { get; }

    /// <summary>
    /// Validates the given PDF (and optional candidate for comparison) and returns a result with findings.
    /// </summary>
    /// <param name="request">Request containing base path, optional candidate path, parsed model, and options.</param>
    /// <param name="cancellationToken">Cancellation token for long-running or parallel runs.</param>
    Task<ValidatorResult> ValidateAsync(PdfValidationContext request, CancellationToken cancellationToken = default);
}

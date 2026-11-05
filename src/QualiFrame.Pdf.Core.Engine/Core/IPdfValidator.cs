using QualiFrame.Pdf.Core.Engine.Core.Contracts;

namespace QualiFrame.Pdf.Core.Engine.Core;

/// <summary>
/// Runs validation rules (text, table, font, image, signature) against a PDF and returns a unified result.
/// </summary>
public interface IPdfValidator
{
    /// <summary>
    /// Run all configured validation rules and return a single result for reporting.
    /// </summary>
    PdfValidationResult Validate(string pdfPath, PdfValidationRequest request);

    /// <summary>
    /// Run validation asynchronously for use in parallel or non-blocking flows.
    /// </summary>
    Task<PdfValidationResult> ValidateAsync(string pdfPath, PdfValidationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request describing which validations to run and their options.
/// </summary>
public class PdfValidationRequest
{
    public IReadOnlyList<ITextValidationRule>? TextRules { get; set; }
    public IReadOnlyList<ITableValidationRule>? TableRules { get; set; }
    public IReadOnlyList<IFontValidationRule>? FontRules { get; set; }
    public IReadOnlyList<IImageValidationRule>? ImageRules { get; set; }
    public IReadOnlyList<ISignatureValidationRule>? SignatureRules { get; set; }
    public PdfExtractionOptions? ExtractionOptions { get; set; }
}

/// <summary>
/// Rule for text validation (clauses, regex, section-based).
/// </summary>
public interface ITextValidationRule
{
    string RuleId { get; }
    PdfTextRuleResult Validate(PdfPageContent[] pages, PdfExtractionResult fullResult);
}

/// <summary>
/// Rule for table structure/content validation.
/// </summary>
public interface ITableValidationRule
{
    string RuleId { get; }
    PdfTableRuleResult Validate(IReadOnlyList<PdfTableInfo> tables, PdfExtractionResult fullResult);
}

/// <summary>
/// Rule for font validation (family, size, style, substitutions).
/// </summary>
public interface IFontValidationRule
{
    string RuleId { get; }
    PdfFontRuleResult Validate(PdfPageContent[] pages, PdfExtractionResult fullResult);
}

/// <summary>
/// Rule for image validation (presence, position, dimensions, fingerprint).
/// </summary>
public interface IImageValidationRule
{
    string RuleId { get; }
    PdfImageRuleResult Validate(IReadOnlyList<PdfImageInfo> images, PdfExtractionResult fullResult);
}

/// <summary>
/// Rule for digital signature validation (presence, certificate, integrity).
/// </summary>
public interface ISignatureValidationRule
{
    string RuleId { get; }
    PdfSignatureRuleResult Validate(string pdfPath, PdfExtractionResult fullResult);
}

// Result types for individual rule categories (simplified; can be extended per rule)
public class PdfTextRuleResult { public bool Passed { get; set; } public string? Message { get; set; } public string? Expected { get; set; } public string? Actual { get; set; } }
public class PdfTableRuleResult { public bool Passed { get; set; } public string? Message { get; set; } }
public class PdfFontRuleResult { public bool Passed { get; set; } public string? Message { get; set; } public int? Page { get; set; } public string? Location { get; set; } }
public class PdfImageRuleResult { public bool Passed { get; set; } public string? Message { get; set; } }
public class PdfSignatureRuleResult { public bool Passed { get; set; } public string? Message { get; set; } public IReadOnlyList<SignatureInfo>? Signatures { get; set; } }

public class SignatureInfo
{
    public bool IsValid { get; set; }
    public string? Issuer { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
    public bool? RevocationChecked { get; set; }
}


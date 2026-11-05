using System.Text.Json.Serialization;

namespace QualiFrame.Pdf.Core.Engine.Config;

/// <summary>
/// Root config for PDF validation (e.g. pdf.validation.json). Strongly typed for schema-like validation.
/// </summary>
public sealed class PdfValidationProfileConfig
{
    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; } = "default";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("validators")]
    public ValidatorTogglesConfig? Validators { get; set; }

    [JsonPropertyName("comparison")]
    public ComparisonOptionsConfig? Comparison { get; set; }

    [JsonPropertyName("parsing")]
    public ParsingOptionsConfig? Parsing { get; set; }

    [JsonPropertyName("font")]
    public FontValidationOptionsConfig? Font { get; set; }

    [JsonPropertyName("signature")]
    public SignatureValidationOptionsConfig? Signature { get; set; }

    [JsonPropertyName("structure")]
    public StructureValidationOptionsConfig? Structure { get; set; }

    [JsonPropertyName("ignoreRegions")]
    public List<IgnoreRegionConfigDto>? IgnoreRegions { get; set; }

    [JsonPropertyName("artifacts")]
    public ArtifactsOptionsConfig? Artifacts { get; set; }
}

/// <summary>Enable/disable validators by id.</summary>
public sealed class ValidatorTogglesConfig
{
    [JsonPropertyName("comparison")]
    public bool Comparison { get; set; } = true;

    [JsonPropertyName("structure")]
    public bool Structure { get; set; } = true;

    [JsonPropertyName("font")]
    public bool Font { get; set; } = true;

    [JsonPropertyName("signature")]
    public bool Signature { get; set; } = false;
}

/// <summary>Comparison thresholds and options.</summary>
public sealed class ComparisonOptionsConfig
{
    [JsonPropertyName("textTolerance")]
    public double TextTolerance { get; set; }

    [JsonPropertyName("visualSimilarityThreshold")]
    public double VisualSimilarityThreshold { get; set; } = 0.99;

    [JsonPropertyName("layoutTolerancePoints")]
    public double LayoutTolerancePoints { get; set; } = 2.0;

    [JsonPropertyName("compareText")]
    public bool CompareText { get; set; } = true;

    [JsonPropertyName("compareLayout")]
    public bool CompareLayout { get; set; } = true;

    [JsonPropertyName("compareVisual")]
    public bool CompareVisual { get; set; } = false;

    [JsonPropertyName("dynamicFieldMasks")]
    public List<DynamicFieldMaskConfigDto>? DynamicFieldMasks { get; set; }
}

/// <summary>Parsing depth options.</summary>
public sealed class ParsingOptionsConfig
{
    [JsonPropertyName("includeStructure")]
    public bool IncludeStructure { get; set; } = true;

    [JsonPropertyName("includeTables")]
    public bool IncludeTables { get; set; } = true;

    [JsonPropertyName("includeImages")]
    public bool IncludeImages { get; set; } = true;

    [JsonPropertyName("includeFontInfo")]
    public bool IncludeFontInfo { get; set; } = true;

    [JsonPropertyName("maxPages")]
    public int? MaxPages { get; set; }
}

/// <summary>Font validation: allowed fonts, sizes, subset/missing rules.</summary>
public sealed class FontValidationOptionsConfig
{
    [JsonPropertyName("allowedFontFamilies")]
    public List<string>? AllowedFontFamilies { get; set; }

    [JsonPropertyName("disallowSubsetFonts")]
    public bool DisallowSubsetFonts { get; set; } = false;

    [JsonPropertyName("requireEmbeddedFonts")]
    public bool RequireEmbeddedFonts { get; set; } = true;

    [JsonPropertyName("minFontSize")]
    public double? MinFontSize { get; set; }

    [JsonPropertyName("maxFontSize")]
    public double? MaxFontSize { get; set; }

    [JsonPropertyName("failOnMissingFonts")]
    public bool FailOnMissingFonts { get; set; } = true;
}

/// <summary>Digital signature requirements.</summary>
public sealed class SignatureValidationOptionsConfig
{
    [JsonPropertyName("requireSignature")]
    public bool RequireSignature { get; set; } = false;

    [JsonPropertyName("validateCertificateChain")]
    public bool ValidateCertificateChain { get; set; } = true;

    [JsonPropertyName("validateTimestamp")]
    public bool ValidateTimestamp { get; set; } = false;

    [JsonPropertyName("allowedSigners")]
    public List<string>? AllowedSigners { get; set; }
}

/// <summary>Structure validation options.</summary>
public sealed class StructureValidationOptionsConfig
{
    [JsonPropertyName("requirePageOrder")]
    public bool RequirePageOrder { get; set; } = true;

    [JsonPropertyName("minBlocksPerPage")]
    public int? MinBlocksPerPage { get; set; }

    [JsonPropertyName("validateTableStructure")]
    public bool ValidateTableStructure { get; set; } = true;
}

/// <summary>Ignore region DTO for JSON.</summary>
public sealed class IgnoreRegionConfigDto
{
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }
}

/// <summary>Dynamic field mask DTO for JSON.</summary>
public sealed class DynamicFieldMaskConfigDto
{
    [JsonPropertyName("regexPattern")]
    public string? RegexPattern { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }
}

/// <summary>Artifact output options.</summary>
public sealed class ArtifactsOptionsConfig
{
    [JsonPropertyName("emitReportJson")]
    public bool EmitReportJson { get; set; } = true;

    [JsonPropertyName("emitParsedModelJson")]
    public bool EmitParsedModelJson { get; set; } = false;

    [JsonPropertyName("emitDiffPdf")]
    public bool EmitDiffPdf { get; set; } = false;

    [JsonPropertyName("emitDiffImages")]
    public bool EmitDiffImages { get; set; } = true;
}

using System.Text.Json;
using QualiFrame.Pdf.Core.Engine.Abstractions;

namespace QualiFrame.Pdf.Core.Engine.Config;

/// <summary>
/// Loads and validates PDF validation profile from JSON (e.g. pdf.validation.json).
/// Provides strongly typed config with helpful errors.
/// </summary>
public static class PdfValidationProfileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Load profile from a JSON file path. Validates and throws with clear message if invalid.
    /// </summary>
    public static IValidationProfile LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"PDF validation profile not found: {jsonPath}", jsonPath);

        var json = File.ReadAllText(jsonPath);
        return LoadFromJson(json, jsonPath);
    }

    /// <summary>
    /// Load profile from JSON string. Source path is used for error messages.
    /// </summary>
    public static IValidationProfile LoadFromJson(string json, string? sourcePath = null)
    {
        PdfValidationProfileConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<PdfValidationProfileConfig>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invalid PDF validation profile JSON{(sourcePath != null ? $" at {sourcePath}" : "")}: {ex.Message}", ex);
        }

        if (config == null)
            throw new InvalidOperationException("PDF validation profile JSON deserialized to null.");

        var errors = ValidateConfig(config);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "PDF validation profile validation failed: " + string.Join("; ", errors));

        return new LoadedValidationProfile(config);
    }

    /// <summary>
    /// Validate config and return list of error messages (empty if valid).
    /// </summary>
    public static IReadOnlyList<string> ValidateConfig(PdfValidationProfileConfig config)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(config.ProfileName))
            errors.Add("profileName is required.");
        if (config.Comparison != null)
        {
            if (config.Comparison.VisualSimilarityThreshold is < 0 or > 1)
                errors.Add("comparison.visualSimilarityThreshold must be between 0 and 1.");
            if (config.Comparison.LayoutTolerancePoints < 0)
                errors.Add("comparison.layoutTolerancePoints must be >= 0.");
        }
        if (config.Font != null)
        {
            if (config.Font.MinFontSize is < 0)
                errors.Add("font.minFontSize must be >= 0.");
            if (config.Font.MaxFontSize is < 0)
                errors.Add("font.maxFontSize must be >= 0.");
        }
        return errors;
    }

    private sealed class LoadedValidationProfile : IValidationProfile
    {
        private readonly PdfValidationProfileConfig _c;

        public LoadedValidationProfile(PdfValidationProfileConfig config)
        {
            _c = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string ProfileName => _c.ProfileName;
        public string? Description => _c.Description;

        public IReadOnlyDictionary<string, bool> ValidatorToggles
        {
            get
            {
                var toggles = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                var v = _c.Validators;
                if (v != null)
                {
                    toggles["Comparison"] = v.Comparison;
                    toggles["Structure"] = v.Structure;
                    toggles["Font"] = v.Font;
                    toggles["Signature"] = v.Signature;
                }
                else
                {
                    toggles["Comparison"] = true;
                    toggles["Structure"] = true;
                    toggles["Font"] = true;
                    toggles["Signature"] = false;
                }
                return toggles;
            }
        }

        public object? GetValidatorOptions(string validatorId)
        {
            if (string.Equals(validatorId, "Comparison", StringComparison.OrdinalIgnoreCase))
                return _c.Comparison;
            if (string.Equals(validatorId, "Structure", StringComparison.OrdinalIgnoreCase))
                return _c.Structure;
            if (string.Equals(validatorId, "Font", StringComparison.OrdinalIgnoreCase))
                return _c.Font;
            if (string.Equals(validatorId, "Signature", StringComparison.OrdinalIgnoreCase))
                return _c.Signature;
            if (string.Equals(validatorId, "Parsing", StringComparison.OrdinalIgnoreCase))
                return _c.Parsing;
            return null;
        }
    }
}

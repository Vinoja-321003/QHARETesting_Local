namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Configurable validation profile: which validators to run, thresholds, allowed fonts, signature requirements, ignore regions.
/// Typically loaded from JSON (e.g. pdf.validation.json).
/// </summary>
public interface IValidationProfile
{
    string ProfileName { get; }
    string? Description { get; }
    IReadOnlyDictionary<string, bool> ValidatorToggles { get; }
    object? GetValidatorOptions(string validatorId);
}

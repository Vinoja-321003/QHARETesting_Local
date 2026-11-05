namespace QualiFrame.Pdf.Core.Engine.Abstractions;

/// <summary>
/// Registry of plugin validators. Adding a new validator must NOT require changes to the engine (open/closed principle).
/// Validators are registered via DI or explicit registration.
/// </summary>
public interface IPdfValidatorRegistry
{
    /// <summary>All registered validators.</summary>
    IReadOnlyList<IPdfValidator> GetAll();

    /// <summary>Get validator by id, or null if not registered.</summary>
    IPdfValidator? GetById(string id);

    /// <summary>Get validators that are enabled in the profile, in deterministic order.</summary>
    IReadOnlyList<IPdfValidator> GetEnabledForProfile(IValidationProfile profile);
}

namespace QualiFrame.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for configuration providers.
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Gets a configuration value by key.
        /// </summary>
        string? GetValue(string key);

        /// <summary>
        /// Gets a configuration value by key with default value.
        /// </summary>
        string GetValue(string key, string defaultValue);

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        bool Validate(out string? errorMessage);
    }
}

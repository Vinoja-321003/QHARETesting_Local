using Microsoft.Extensions.Configuration;
using QualiFrame.Mocking.Config;

namespace QualiFrame.Mocking.Utilities;

/// <summary>
/// Manages the mocking ON/OFF toggle from config (appsettings, env vars, command-line, CI).
/// </summary>
public class EnvironmentToggleManager
{
    private readonly IConfiguration _configuration;
    private MockServerConfig? _cachedConfig;

    public EnvironmentToggleManager(IConfiguration? configuration = null)
    {
        _configuration = configuration ?? new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();
    }

    /// <summary>
    /// Whether mocking is enabled (from Mocking:Enabled or MOCKING_ENABLED, etc.).
    /// </summary>
    public bool IsMockingEnabled
    {
        get
        {
            var value = _configuration["Mocking:Enabled"]
                        ?? Environment.GetEnvironmentVariable("MOCKING_ENABLED")
                        ?? _configuration["Mocking__Enabled"];
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
    }

    /// <summary>
    /// Current mocking mode (Off, Static, Dynamic, Hybrid).
    /// </summary>
    public MockingMode Mode
    {
        get
        {
            var value = _configuration["Mocking:Mode"]
                        ?? Environment.GetEnvironmentVariable("MOCKING_MODE")
                        ?? _configuration["Mocking__Mode"]
                        ?? "Off";
            return Enum.TryParse<MockingMode>(value, true, out var mode) ? mode : MockingMode.Off;
        }
    }

    /// <summary>
    /// Whether to use Docker for WireMock.
    /// </summary>
    public bool UseDocker
    {
        get
        {
            var value = _configuration["Mocking:UseDocker"]
                        ?? Environment.GetEnvironmentVariable("MOCKING_USE_DOCKER")
                        ?? "false";
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
    }

    /// <summary>
    /// Gets the full mock server configuration (section Mocking).
    /// </summary>
    public MockServerConfig GetMockServerConfig()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        var section = _configuration.GetSection(MockServerConfig.SectionName);
        _cachedConfig = new MockServerConfig
        {
            Enabled = section.GetValue<bool?>("Enabled") ?? bool.TryParse(_configuration["Mocking:Enabled"], out var e) && e,
            Mode = Enum.TryParse<MockingMode>(section["Mode"] ?? _configuration["Mocking:Mode"] ?? "Off", true, out var m) ? m : MockingMode.Off,
            UseDocker = section.GetValue<bool?>("UseDocker") ?? UseDocker,
            Port = section.GetValue<int?>("Port") ?? (int.TryParse(Environment.GetEnvironmentVariable("MOCKING_PORT"), out var p) ? p : 9091),
            BaseUrl = section["BaseUrl"] ?? Environment.GetEnvironmentVariable("MOCKING_BASE_URL") ?? "http://localhost:9091",
            SwaggerPath = section["SwaggerPath"] ?? _configuration["Mocking:SwaggerPath"],
            ValidateContracts = section.GetValue<bool?>("ValidateContracts") ?? true,
            GenerateFromOpenApi = section.GetValue<bool?>("GenerateFromOpenApi") ?? true,
            ResetMappingsBetweenTests = section.GetValue<bool?>("ResetMappingsBetweenTests") ?? true
        };
        return _cachedConfig;
    }

    /// <summary>
    /// Clears cached config (e.g. after config reload).
    /// </summary>
    public void ClearCache() => _cachedConfig = null;
}

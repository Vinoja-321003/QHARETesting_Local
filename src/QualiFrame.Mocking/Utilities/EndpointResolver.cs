using QualiFrame.Mocking.Config;

namespace QualiFrame.Mocking.Utilities;

/// <summary>
/// Resolves API base URLs: when mocking is enabled returns WireMock base URL; otherwise real service URL.
/// </summary>
public class EndpointResolver
{
    private readonly EnvironmentToggleManager _toggle;
    private readonly MockServerConfig _mockConfig;
    private readonly Dictionary<string, string> _realEndpoints = new(StringComparer.OrdinalIgnoreCase);

    public EndpointResolver(
        EnvironmentToggleManager? toggle = null,
        MockServerConfig? mockConfig = null)
    {
        _toggle = toggle ?? new EnvironmentToggleManager();
        _mockConfig = mockConfig ?? _toggle.GetMockServerConfig();
    }

    /// <summary>
    /// Registers a named API and its real base URL.
    /// </summary>
    public EndpointResolver RegisterRealEndpoint(string name, string baseUrl)
    {
        _realEndpoints[name] = baseUrl.TrimEnd('/');
        return this;
    }

    /// <summary>
    /// Resolves the base URL for a named API. When mocking is enabled returns mock server URL; otherwise real.
    /// </summary>
    public string Resolve(string apiName)
    {
        if (_toggle.IsMockingEnabled && _mockConfig.Enabled)
            return _mockConfig.BaseUrl.TrimEnd('/');

        if (_realEndpoints.TryGetValue(apiName, out var real))
            return real;

        return _mockConfig.BaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Resolves with a fallback when the named API is not registered.
    /// </summary>
    public string Resolve(string apiName, string fallbackRealUrl)
    {
        if (_toggle.IsMockingEnabled && _mockConfig.Enabled)
            return _mockConfig.BaseUrl.TrimEnd('/');
        return _realEndpoints.TryGetValue(apiName, out var real) ? real : fallbackRealUrl.TrimEnd('/');
    }
}

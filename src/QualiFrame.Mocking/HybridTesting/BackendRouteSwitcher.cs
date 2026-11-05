using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Server;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.HybridTesting;

/// <summary>
/// Switches the application/backend base URL to the mock server or real service (config/env driven).
/// </summary>
public class BackendRouteSwitcher
{
    private readonly EndpointResolver _resolver;
    private readonly MockServerLifecycle _lifecycle;
    private readonly EnvironmentToggleManager _toggle;
    private string? _currentMockBaseUrl;

    public BackendRouteSwitcher(
        EndpointResolver? resolver = null,
        MockServerLifecycle? lifecycle = null,
        EnvironmentToggleManager? toggle = null)
    {
        _toggle = toggle ?? new EnvironmentToggleManager();
        var config = _toggle.GetMockServerConfig();
        _resolver = resolver ?? new EndpointResolver(_toggle, config);
        _lifecycle = lifecycle ?? new MockServerLifecycle(config);
    }

    /// <summary>
    /// Ensures mock server is started and returns the base URL to use for the app/API.
    /// </summary>
    public async Task<string> EnsureBackendUrlAsync(string apiName, CancellationToken cancellationToken = default)
    {
        if (!_toggle.IsMockingEnabled)
            return _resolver.Resolve(apiName, "http://localhost:5000");

        if (string.IsNullOrEmpty(_currentMockBaseUrl))
            _currentMockBaseUrl = await _lifecycle.StartAsync(cancellationToken: cancellationToken);

        return _currentMockBaseUrl;
    }

    public string EnsureBackendUrl(string apiName) =>
        EnsureBackendUrlAsync(apiName).GetAwaiter().GetResult();

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.StopAsync(cancellationToken);
        _currentMockBaseUrl = null;
    }

    public void RegisterStubs(Action<WireMockServerManager> register) =>
        register(_lifecycle.InProcessManager);
}

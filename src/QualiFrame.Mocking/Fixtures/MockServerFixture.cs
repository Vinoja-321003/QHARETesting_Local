using NUnit.Framework;
using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Server;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.Fixtures;

/// <summary>
/// NUnit fixture: one-time start/stop of WireMock server; per-test stub registration/reset.
/// Compatible with xUnit/MSTest by using the same lifecycle in constructor/Dispose or IClassFixture.
/// </summary>
public class MockServerFixture : IDisposable
{
    private readonly MockServerConfig _config;
    private readonly WireMockServerManager _manager;
    private readonly bool _useDocker;
    private MockServerLifecycle? _lifecycle;

    public MockServerFixture(MockServerConfig? config = null, bool useDocker = false)
    {
        _config = config ?? new EnvironmentToggleManager().GetMockServerConfig();
        _useDocker = useDocker || _config.UseDocker;
        _manager = new WireMockServerManager(_config);
    }

    /// <summary>WireMock server manager for registering stubs.</summary>
    public WireMockServerManager Manager => _manager;

    /// <summary>Base URL of the mock server.</summary>
    public string BaseUrl => _lifecycle?.BaseUrl ?? _manager.BaseUrl;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (_useDocker)
        {
            _lifecycle = new MockServerLifecycle(_config);
            _lifecycle.StartAsync().GetAwaiter().GetResult();
        }
        else
        {
            _manager.Start();
        }
    }

    [SetUp]
    public void SetUp()
    {
        if (!_useDocker && _config.ResetMappingsBetweenTests)
            _manager.ResetMappings();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_lifecycle != null)
            _lifecycle.StopAsync().GetAwaiter().GetResult();
        else
            _manager.Stop();
    }

    public void Dispose() => OneTimeTearDown();
}

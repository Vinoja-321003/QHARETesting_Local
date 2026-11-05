using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.Server;

/// <summary>
/// Coordinates mock server lifecycle: start (in-process or Docker), apply config mode, generate/register stubs, teardown.
/// </summary>
public class MockServerLifecycle
{
    private readonly MockServerConfig _config;
    private readonly OpenApiMockGenerator _generator;
    private readonly WireMockServerManager _inProcessManager;
    private readonly DockerWireMockManager? _dockerManager;
    private readonly LoggingHelper _log;

    public MockServerLifecycle(
        MockServerConfig? config = null,
        OpenApiMockGenerator? generator = null,
        WireMockServerManager? inProcessManager = null,
        DockerWireMockManager? dockerManager = null,
        LoggingHelper? log = null)
    {
        _config = config ?? new MockServerConfig();
        _generator = generator ?? new OpenApiMockGenerator();
        _inProcessManager = inProcessManager ?? new WireMockServerManager(_config);
        _dockerManager = _config.UseDocker ? (dockerManager ?? new DockerWireMockManager(new DockerConfig(), log)) : null;
        _log = log ?? new LoggingHelper();
    }

    /// <summary>
    /// Starts the mock server (Docker or in-process) and optionally generates stubs from OpenAPI.
    /// </summary>
    public async Task<string> StartAsync(
        bool generateFromOpenApi = true,
        string? swaggerPath = null,
        IReadOnlyList<string>? includePaths = null,
        IReadOnlyList<StubDefinition>? customOverrides = null,
        Action<WireMockServerManager>? registerStaticStubs = null,
        CancellationToken cancellationToken = default)
    {
        string baseUrl;
        if (_config.UseDocker && _dockerManager != null)
        {
            baseUrl = await _dockerManager.StartAsync(cancellationToken);
            if (generateFromOpenApi && !string.IsNullOrEmpty(swaggerPath ?? _config.SwaggerPath))
            {
                var specPath = swaggerPath ?? _config.SwaggerPath!;
                if (File.Exists(specPath))
                {
                    var stubs = _generator.GenerateStubDefinitionsFromSpec(specPath, includePaths, customOverrides);
                    await RegisterStubsToRemoteAsync(baseUrl, stubs, cancellationToken);
                }
            }
            return baseUrl;
        }

        _inProcessManager.Start();
        baseUrl = _inProcessManager.BaseUrl;

        if (_config.ResetMappingsBetweenTests)
            _inProcessManager.ResetMappings();

        if (generateFromOpenApi && (_config.Mode == MockingMode.Dynamic || _config.Mode == MockingMode.Hybrid) && !string.IsNullOrEmpty(swaggerPath ?? _config.SwaggerPath))
        {
            var specPath = swaggerPath ?? _config.SwaggerPath!;
            if (File.Exists(specPath))
            {
                var count = _generator.GenerateMocksFromSpec(specPath, _inProcessManager.RegisterStub, includePaths, customOverrides);
                _log.Info("Generated mocks from OpenAPI", ("SpecPath", specPath), ("Count", count));
            }
        }

        registerStaticStubs?.Invoke(_inProcessManager);
        return baseUrl;
    }

    /// <summary>
    /// Synchronous start for in-process only (no Docker).
    /// </summary>
    public string Start(
        bool generateFromOpenApi = true,
        string? swaggerPath = null,
        IReadOnlyList<string>? includePaths = null,
        IReadOnlyList<StubDefinition>? customOverrides = null,
        Action<WireMockServerManager>? registerStaticStubs = null)
    {
        return StartAsync(generateFromOpenApi, swaggerPath, includePaths, customOverrides, registerStaticStubs).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Stops the mock server.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_config.UseDocker && _dockerManager != null)
            await _dockerManager.StopAsync(cancellationToken);
        _inProcessManager.Stop();
    }

    /// <summary>Current base URL of the mock server.</summary>
    public string BaseUrl => _config.UseDocker && _dockerManager != null ? _dockerManager.BaseUrl : _inProcessManager.BaseUrl;

    /// <summary>Manager for in-process server (for adding stubs at runtime).</summary>
    public WireMockServerManager InProcessManager => _inProcessManager;

    private static async Task RegisterStubsToRemoteAsync(string baseUrl, IReadOnlyList<StubDefinition> stubs, CancellationToken ct)
    {
        foreach (var s in stubs)
            await RegisterStubToRemoteAsync(baseUrl, s, ct);
    }

    private static async Task RegisterStubToRemoteAsync(string baseUrl, StubDefinition stub, CancellationToken ct)
    {
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var mapping = new
        {
            request = new { method = stub.Method, urlPath = stub.Path },
            response = new
            {
                status = stub.StatusCode,
                body = stub.ResponseBody,
                headers = stub.ResponseHeaders,
                delay = stub.DelayMs.HasValue ? stub.DelayMs / 1000.0 : (double?)null
            }
        };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(mapping);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var r = await client.PostAsync("/__admin/mappings", content, ct);
        r.EnsureSuccessStatusCode();
    }
}

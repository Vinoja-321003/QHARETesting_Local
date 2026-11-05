using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Matchers;
using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Generation;

namespace QualiFrame.Mocking.Server;

/// <summary>
/// Manages the lifecycle of an in-process WireMock server: start, stop, reset mappings, register stubs.
/// </summary>
public class WireMockServerManager : IDisposable
{
    private WireMockServer? _server;
    private readonly MockServerConfig _config;
    private readonly object _lock = new();

    public WireMockServerManager(MockServerConfig? config = null)
    {
        _config = config ?? new MockServerConfig();
    }

    /// <summary>Current WireMock server instance (null if not started).</summary>
    public WireMockServer? Server => _server;

    /// <summary>Base URL of the mock server (e.g. http://localhost:9091).</summary>
    public string BaseUrl => _server?.Url ?? _config.BaseUrl;

    /// <summary>Whether the server is running.</summary>
    public bool IsRunning => _server?.IsStarted == true;

    /// <summary>
    /// Starts the WireMock server with configured host and port.
    /// </summary>
    public string Start()
    {
        lock (_lock)
        {
            if (_server?.IsStarted == true)
                return _server.Url!;

            _server = WireMockServer.Start(_config.Port);
            return _server.Url!;
        }
    }

    /// <summary>
    /// Stops the server and disposes it.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _server?.Stop();
            _server?.Dispose();
            _server = null;
        }
    }

    /// <summary>
    /// Resets all mappings (and optionally request logs). Use between tests for isolation.
    /// </summary>
    public void ResetMappings()
    {
        _server?.ResetMappings();
    }

    /// <summary>
    /// Registers a stub definition with the WireMock server.
    /// Paths containing {param} are converted to path patterns so /api/users/{id} matches /api/users/1.
    /// </summary>
    public void RegisterStub(StubDefinition stub)
    {
        if (_server == null)
            throw new InvalidOperationException("WireMock server is not started. Call Start() first.");

        var (pathPattern, useRegex) = stub.Path.Contains('{')
            ? (OpenApiPathToRegexPattern(stub.Path), true)
            : (stub.Path, false);

        IRequestBuilder request = Request.Create();
        if (useRegex)
            request = request.WithPath(new RegexMatcher(pathPattern));
        else
            request = request.WithPath(pathPattern);

        request = stub.Method.ToUpperInvariant() switch
        {
            "GET" => request.UsingGet(),
            "POST" => request.UsingPost(),
            "PUT" => request.UsingPut(),
            "PATCH" => request.UsingPatch(),
            "DELETE" => request.UsingDelete(),
            _ => request.UsingAnyMethod()
        };

        var response = Response.Create()
            .WithStatusCode(stub.StatusCode)
            .WithHeader("Content-Type", "application/json");

        if (stub.ResponseBody != null)
            response = response.WithBody(stub.ResponseBody);
        if (stub.ResponseHeaders != null)
        {
            foreach (var h in stub.ResponseHeaders)
                response = response.WithHeader(h.Key, h.Value);
        }
        if (stub.DelayMs.HasValue && stub.DelayMs.Value > 0)
            response = response.WithDelay(TimeSpan.FromMilliseconds(stub.DelayMs.Value));

        _server.Given(request).RespondWith(response);
    }

    /// <summary>
    /// Converts OpenAPI path like /api/users/{id} to a regex pattern string so it matches /api/users/1.
    /// When path has no {param}, the caller should pass useRegex: false (exact match).
    /// </summary>
    private static string OpenApiPathToRegexPattern(string path)
    {
        var pattern = System.Text.RegularExpressions.Regex.Replace(path, @"\{[^}]+\}", @"[^/]+");
        return "^" + pattern + "$";
    }

    /// <summary>
    /// Registers multiple stubs.
    /// </summary>
    public void RegisterStubs(IEnumerable<StubDefinition> stubs)
    {
        foreach (var s in stubs)
            RegisterStub(s);
    }

    public void Dispose() => Stop();
}

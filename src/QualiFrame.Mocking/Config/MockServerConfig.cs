namespace QualiFrame.Mocking.Config;

/// <summary>
/// Configuration for the WireMock mock server (host, port, lifecycle, and behavior).
/// </summary>
public class MockServerConfig
{
    public const string SectionName = "Mocking";

    /// <summary>Whether mocking is enabled (use mocks vs real backend).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Mocking mode: Off, Static, Dynamic, Hybrid.</summary>
    public MockingMode Mode { get; set; } = MockingMode.Off;

    /// <summary>Use Dockerized WireMock when true; otherwise in-process.</summary>
    public bool UseDocker { get; set; } = false;

    /// <summary>Host to bind (e.g. localhost or *).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Port for the mock server.</summary>
    public int Port { get; set; } = 9091;

    /// <summary>Base URL where the mock server is reachable (e.g. http://localhost:9091).</summary>
    public string BaseUrl { get; set; } = "http://localhost:9091";

    /// <summary>Path to OpenAPI/Swagger spec (JSON or YAML).</summary>
    public string? SwaggerPath { get; set; }

    /// <summary>Validate responses against OpenAPI contract when true.</summary>
    public bool ValidateContracts { get; set; } = true;

    /// <summary>Generate WireMock stubs from OpenAPI when in Dynamic/Hybrid mode.</summary>
    public bool GenerateFromOpenApi { get; set; } = true;

    /// <summary>Reset mappings between tests for isolation.</summary>
    public bool ResetMappingsBetweenTests { get; set; } = true;

    /// <summary>Log incoming requests for diagnostics.</summary>
    public bool LogIncomingRequests { get; set; } = true;

    /// <summary>Log unmatched requests.</summary>
    public bool LogUnmatchedRequests { get; set; } = true;

    /// <summary>Admin username for WireMock admin API (optional).</summary>
    public string? AdminUsername { get; set; }

    /// <summary>Admin password for WireMock admin API (optional).</summary>
    public string? AdminPassword { get; set; }
}

/// <summary>
/// Mocking mode: Off = real backend; Static = manual stubs only; Dynamic = OpenAPI-generated; Hybrid = generated + overrides.
/// </summary>
public enum MockingMode
{
    Off,
    Static,
    Dynamic,
    Hybrid
}

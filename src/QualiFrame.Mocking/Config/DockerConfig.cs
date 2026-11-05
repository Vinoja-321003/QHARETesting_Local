namespace QualiFrame.Mocking.Config;

/// <summary>
/// Configuration for running WireMock in Docker (CI/CD and local).
/// </summary>
public class DockerConfig
{
    public const string SectionName = "Mocking:Docker";

    /// <summary>Docker image for WireMock (e.g. wiremock/wiremock).</summary>
    public string Image { get; set; } = "wiremock/wiremock:3.9.2";

    /// <summary>Container name for the mock server.</summary>
    public string ContainerName { get; set; } = "qualiframe-wiremock";

    /// <summary>Host port to map to container's WireMock port.</summary>
    public int HostPort { get; set; } = 9091;

    /// <summary>Path on host to mount as mappings (optional).</summary>
    public string? MappingsMountPath { get; set; }

    /// <summary>Path on host to mount as __files (optional).</summary>
    public string? FilesMountPath { get; set; }

    /// <summary>Docker API endpoint (default: local Docker).</summary>
    public string? DockerUri { get; set; }

    /// <summary>Startup timeout in seconds.</summary>
    public int StartupTimeoutSeconds { get; set; } = 30;

    /// <summary>Health check interval in seconds.</summary>
    public int HealthCheckIntervalSeconds { get; set; } = 2;

    /// <summary>Remove container on stop.</summary>
    public bool RemoveContainerOnStop { get; set; } = true;
}

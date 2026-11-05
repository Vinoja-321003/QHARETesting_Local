using Docker.DotNet;
using Docker.DotNet.Models;
using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Utilities;

namespace QualiFrame.Mocking.Server;

/// <summary>
/// Manages WireMock server running inside a Docker container for CI/CD and local use.
/// </summary>
public class DockerWireMockManager
{
    private readonly DockerConfig _config;
    private readonly LoggingHelper _log;
    private string? _containerId;

    public DockerWireMockManager(DockerConfig? config = null, LoggingHelper? log = null)
    {
        _config = config ?? new DockerConfig();
        _log = log ?? new LoggingHelper();
    }

    /// <summary>Base URL to reach the mock server (host:HostPort).</summary>
    public string BaseUrl => $"http://localhost:{_config.HostPort}";

    /// <summary>Whether the container is running.</summary>
    public bool IsRunning => !string.IsNullOrEmpty(_containerId);

    /// <summary>
    /// Starts the WireMock container. Optionally mounts mappings/__files from host paths.
    /// </summary>
    public async Task<string> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_containerId))
            return BaseUrl;

        var dockerUri = _config.DockerUri != null ? new Uri(_config.DockerUri) : null;
        var dockerConfig = new DockerClientConfiguration(dockerUri);
        using var dockerClient = dockerConfig.CreateClient();

        var createParams = new CreateContainerParameters
        {
            Image = _config.Image,
            Name = _config.ContainerName,
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["8080"] = new List<PortBinding>
                    {
                        new() { HostIP = "127.0.0.1", HostPort = _config.HostPort.ToString() }
                    }
                }
            },
            ExposedPorts = new Dictionary<string, EmptyStruct> { ["8080"] = default }
        };

        if (!string.IsNullOrEmpty(_config.MappingsMountPath) && Directory.Exists(_config.MappingsMountPath))
        {
            createParams.HostConfig.Binds ??= new List<string>();
            createParams.HostConfig.Binds.Add($"{Path.GetFullPath(_config.MappingsMountPath)}:/home/wiremock/mappings");
        }
        if (!string.IsNullOrEmpty(_config.FilesMountPath) && Directory.Exists(_config.FilesMountPath))
        {
            createParams.HostConfig.Binds ??= new List<string>();
            createParams.HostConfig.Binds.Add($"{Path.GetFullPath(_config.FilesMountPath)}:/home/wiremock/__files");
        }

        var response = await dockerClient.Containers.CreateContainerAsync(createParams, cancellationToken);
        _containerId = response.ID;
        await dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters(), cancellationToken);

        _log.Info("Docker WireMock started", ("ContainerId", _containerId), ("BaseUrl", BaseUrl));

        await WaitForHealthyAsync(cancellationToken);
        return BaseUrl;
    }

    /// <summary>
    /// Stops and optionally removes the container.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_containerId)) return;

        try
        {
            var dockerUri = _config.DockerUri != null ? new Uri(_config.DockerUri) : null;
            var dockerConfig = new DockerClientConfiguration(dockerUri);
            using var dockerClient = dockerConfig.CreateClient();
            await dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters(), cancellationToken);
            if (_config.RemoveContainerOnStop)
                await dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters(), cancellationToken);
        }
        finally
        {
            _containerId = null;
        }
    }

    private async Task WaitForHealthyAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_config.StartupTimeoutSeconds);
        using var http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(5) };

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var r = await http.GetAsync("/__admin/mappings", cancellationToken);
                if (r.IsSuccessStatusCode) return;
            }
            catch { /* retry */ }
            await Task.Delay(TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds), cancellationToken);
        }

        throw new TimeoutException($"WireMock container did not become healthy within {_config.StartupTimeoutSeconds}s.");
    }
}

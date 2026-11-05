# QualiFrame.Mocking

API mocking and service virtualization layer for Selenium C# tests. Enables tests to run against **real backend services** or **dynamically generated WireMock.Net mocks** derived from OpenAPI/Swagger contracts.

## Features

- **WireMock.Net integration**: Start/stop server (port-based), configurable host/port, reset mappings between tests, register stubs programmatically. Path matching supports exact paths and regex for OpenAPI path parameters (e.g. `/api/users/{id}`).
- **OpenAPI-driven mocks**: Load Swagger/OpenAPI (JSON or YAML), generate WireMock stubs for paths/methods/parameters/responses; use examples or schema-based defaults.
- **Contract validation**: Validate response status codes and body schema against the OpenAPI spec; detect drift between mocks and contract. Uses string-based schema type checks for compatibility across Microsoft.OpenApi versions.
- **Configuration-driven toggle**: Enable/disable mocking via `appsettings.json`, environment variables, CI pipeline variables, or command-line.
- **Dockerized WireMock**: Run mock server in Docker for local, QA, and CI/CD with health checks and clean startup/shutdown. Uses Docker.DotNet (`DockerClientConfiguration`, `CreateContainerParameters`).
- **Hybrid API + UI testing**: Orchestrate mock server + stub registration + UI workflow + API contract validation in one flow.
- **Reusable domain stubs**: Prebuilt stubs for User, Order, Payment APIs (success, 4xx/5xx, delayed responses) with override support.

## Implementation notes

- **Schema validation**: `SchemaValidationService` and `ResponseTemplateBuilder` use `schema.Type?.ToString()` compared to `"Object"`, `"Array"`, `"String"`, `"Integer"`, `"Number"`, `"Boolean"` so the code works across Microsoft.OpenApi versions regardless of enum type (SchemaType vs JsonSchemaType).
- **WireMock path matching**: Exact paths use `WithPath(path)`; paths with `{param}` are converted to a regex pattern and registered with `WireMock.Matchers.RegexMatcher`.
- **WireMock server**: Started with `WireMockServer.Start(port)` (no custom settings object). Admin interface is enabled by default.
- **Docker**: Uses `Docker.DotNet` with `DockerClientConfiguration(dockerUri).CreateClient()` and `CreateContainerParameters` / `HostConfig` / `PortBinding` from `Docker.DotNet.Models`.

## Architecture

```
QualiFrame.Mocking
├── Config          MockServerConfig, OpenApiConfig, DockerConfig
├── Contracts       OpenApiSpecLoader, ContractValidator, SchemaValidationService
├── Generation      OpenApiMockGenerator, ResponseTemplateBuilder, StubDefinitionFactory
├── Server         WireMockServerManager, DockerWireMockManager, MockServerLifecycle
├── Stubs          UserApiStubs, OrderApiStubs, PaymentApiStubs, DynamicStubRegistry
├── HybridTesting  HybridTestOrchestrator, ApiUiValidationService, BackendRouteSwitcher
├── Fixtures       MockServerFixture, HybridTestFixture, ContractValidationFixture
└── Utilities      EnvironmentToggleManager, EndpointResolver, LoggingHelper

Sample integration tests: `QualiFrame.Tests/Tests/Integration/Mocking/` (`Category=Mocking`).
```

## Installation

Add a project reference from your test project:

```xml
<ProjectReference Include="..\QualiFrame.Mocking\QualiFrame.Mocking.csproj" />
```

Or install from NuGet (when published):

```bash
dotnet add package QualiFrame.Mocking
```

## Configuration

### appsettings.json

```json
{
  "Mocking": {
    "Enabled": true,
    "Mode": "Dynamic",
    "UseDocker": false,
    "Port": 9091,
    "BaseUrl": "http://localhost:9091",
    "SwaggerPath": "Contracts/swagger.json",
    "ValidateContracts": true,
    "GenerateFromOpenApi": true,
    "ResetMappingsBetweenTests": true
  }
}
```

**Modes**

- **Off** – Use real backend; mocking disabled.
- **Static** – Use only manually defined stubs (no OpenAPI generation).
- **Dynamic** – Generate all mocks from OpenAPI spec.
- **Hybrid** – Generated baseline mocks + custom overrides.

### Environment variables

- `MOCKING_ENABLED` = true | false  
- `MOCKING_MODE` = Off | Static | Dynamic | Hybrid  
- `MOCKING_USE_DOCKER` = true | false  
- `MOCKING_PORT` = 9091  
- `MOCKING_BASE_URL` = http://localhost:9091  
- `MOCKING_SWAGGER_PATH` = path to swagger.json  

CI can set these so the same tests run against real or mocked backend without code changes.

## Usage

### 1. WireMock server lifecycle

```csharp
using QualiFrame.Mocking.Server;

var config = new MockServerConfig { Port = 9091, BaseUrl = "http://localhost:9091" };
var manager = new WireMockServerManager(config);
manager.Start();

manager.RegisterStub(StubDefinitionFactory.CreateCustom("/api/users/1", "GET", 200, "{\"id\":1,\"name\":\"Jane\"}"));
// run tests against manager.BaseUrl
manager.ResetMappings(); // between tests
manager.Stop();
```

### 2. Dynamic mocks from OpenAPI

```csharp
using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Server;

var manager = new WireMockServerManager();
manager.Start();

var generator = new OpenApiMockGenerator();
generator.GenerateMocksFromSpec("Contracts/swagger.json", manager.RegisterStub);
// optional: only certain paths
generator.GenerateMocksFromSpec("Contracts/swagger.json", manager.RegisterStub,
    includePaths: new[] { "/api/users", "/api/orders" });
// optional: custom overrides
var overrides = new[] { StubDefinitionFactory.CreateCustom("/api/users/1", "GET", 200, customJson) };
generator.GenerateMocksFromSpec("Contracts/swagger.json", manager.RegisterStub, overrideStubs: overrides);
```

### 3. Contract validation

```csharp
using QualiFrame.Mocking.Contracts;

var validator = new ContractValidator();
var result = validator.ValidateResponseAgainstSpec(
    "Contracts/swagger.json", "/api/users/1", "GET", 200, responseBodyJson);
Assert.That(result.IsValid, result.Message);
```

### 4. Hybrid UI + API flow

```csharp
using QualiFrame.Mocking.HybridTesting;
using QualiFrame.Mocking.Stubs;

var orchestrator = new HybridTestOrchestrator();
orchestrator.ExecuteMockedUiFlow(
    registerStubs: m => UserApiStubs.RegisterGetUserSuccess(m, "1"),
    testAction: baseUrl =>
    {
        // Point app to baseUrl; run Selenium; call API; assert UI
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var response = client.GetAsync("/api/users/1").Result;
        // assert response + UI
    },
    swaggerPath: "Contracts/swagger.json",
    validateContractPath: "/api/users/1",
    validateContractMethod: "GET",
    capturedResponseStatusCode: 200,
    capturedResponseBody: responseBody);
```

### 5. Endpoint resolution (real vs mock)

```csharp
using QualiFrame.Mocking.Utilities;

var resolver = new EndpointResolver();
resolver.RegisterRealEndpoint("usersApi", "https://api.example.com");
string url = resolver.Resolve("usersApi"); // mock URL if mocking enabled, else real
```

### 6. Fixtures (NUnit)

```csharp
[TestFixture]
public class MyTests : MockServerFixture
{
    [Test]
    public void Test()
    {
        UserApiStubs.RegisterGetUserSuccess(Manager, "1");
        // test against BaseUrl
    }
}
```

## Docker

### Local / CI

From repo root:

```bash
docker-compose -f docker-compose.wiremock.yml up -d
```

Then set `MOCKING_BASE_URL=http://localhost:9091` (or the host port you map). Tests that use `EnvironmentToggleManager` / `MockServerConfig` will use this URL when mocking is enabled.

### Using DockerWireMockManager in code

```csharp
var dockerConfig = new DockerConfig { HostPort = 9091, Image = "wiremock/wiremock:3.9.2" };
var dockerManager = new DockerWireMockManager(dockerConfig);
await dockerManager.StartAsync();
// register mappings via Admin API or pre-mounted files
string baseUrl = dockerManager.BaseUrl;
await dockerManager.StopAsync();
```

### Health check

The Dockerfile and compose use a healthcheck that hits `/__admin/mappings`. Wait for the container to be healthy before running tests.

## Adding new APIs and stubs

1. **New domain stubs**: Add a static class under `Stubs/` (e.g. `ProductApiStubs`) with methods that call `manager.RegisterStub(StubDefinitionFactory.CreateCustom(...))` for success, 4xx, 5xx, and delayed responses.
2. **New OpenAPI spec**: Place `swagger.json` or `swagger.yaml` under `Contracts/` (or path in config). Use `OpenApiMockGenerator.GenerateMocksFromSpec` with optional `includePaths` and `overrideStubs`.
3. **New test flows**: Use `HybridTestOrchestrator.ExecuteMockedUiFlow` or `MockServerFixture` + domain stubs; add contract validation when you have a spec.

## Extension points

- **Logging**: Replace or extend `LoggingHelper` to integrate with Serilog, Allure, or QualiFrame reporting.
- **Config**: Implement a custom config source and pass `IConfiguration` into `EnvironmentToggleManager`.
- **Stub persistence**: Use WireMock Admin API to export/import mappings; mount them in Docker for repeatable runs.

## Changelog (QualiFrame.Mocking)

- **v3.0.0**: Initial release. WireMock.Net integration, OpenAPI mock generation, contract validation (string-based schema type checks for cross-version compatibility), Docker support via Docker.DotNet, hybrid testing orchestration, domain stubs (User, Order, Payment), fixtures for NUnit.

## License

Part of QualiFrame. See solution license.

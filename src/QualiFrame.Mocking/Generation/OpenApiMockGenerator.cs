using Microsoft.OpenApi.Models;
using QualiFrame.Mocking.Config;
using QualiFrame.Mocking.Contracts;

namespace QualiFrame.Mocking.Generation;

/// <summary>
/// Generates WireMock.Net stubs from an OpenAPI/Swagger specification.
/// Supports full-spec or selected paths, with optional custom overrides.
/// </summary>
public class OpenApiMockGenerator
{
    private readonly OpenApiSpecLoader _loader;
    private readonly StubDefinitionFactory _stubFactory;
    private readonly OpenApiConfig? _openApiConfig;

    public OpenApiMockGenerator(
        OpenApiSpecLoader? loader = null,
        StubDefinitionFactory? stubFactory = null,
        OpenApiConfig? openApiConfig = null)
    {
        _loader = loader ?? new OpenApiSpecLoader();
        _stubFactory = stubFactory ?? new StubDefinitionFactory();
        _openApiConfig = openApiConfig;
    }

    /// <summary>
    /// Generates stub definitions from an OpenAPI spec file. Registers them with the given action.
    /// </summary>
    /// <param name="specPath">Path to swagger.json or swagger.yaml</param>
    /// <param name="registerStub">Action to register each generated stub (e.g. with WireMockServer)</param>
    /// <param name="includePaths">Optional: only these paths (e.g. "/api/users", "/api/orders"). Empty = all.</param>
    /// <param name="overrideStubs">Optional: custom stubs that take precedence over generated ones</param>
    /// <returns>Number of stubs generated</returns>
    public int GenerateMocksFromSpec(
        string specPath,
        Action<StubDefinition> registerStub,
        IReadOnlyList<string>? includePaths = null,
        IReadOnlyList<StubDefinition>? overrideStubs = null)
    {
        var load = _loader.LoadFromFile(specPath);
        if (!load.IsSuccess || load.Document == null)
            throw new InvalidOperationException($"Failed to load OpenAPI spec: {load.ErrorMessage}");

        return GenerateMocksFromDocument(load.Document, registerStub, includePaths, overrideStubs);
    }

    /// <summary>
    /// Generates stub definitions from an in-memory OpenAPI document.
    /// </summary>
    public int GenerateMocksFromDocument(
        OpenApiDocument document,
        Action<StubDefinition> registerStub,
        IReadOnlyList<string>? includePaths = null,
        IReadOnlyList<StubDefinition>? overrideStubs = null)
    {
        var overrides = (overrideStubs ?? Array.Empty<StubDefinition>())
            .ToDictionary(s => $"{s.Method}:{NormalizePath(s.Path)}", StringComparer.OrdinalIgnoreCase);

        var allowedMethods = _openApiConfig?.AllowedMethods ?? new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
        var excludePaths = _openApiConfig?.ExcludePaths ?? Array.Empty<string>();
        var count = 0;

        foreach (var pathItem in document.Paths)
        {
            var path = "/" + pathItem.Key.TrimStart('/');
            if (includePaths != null && includePaths.Count > 0 && !includePaths.Any(p => path.StartsWith(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
                continue;
            if (excludePaths.Any(p => path.StartsWith(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var op in pathItem.Value.Operations)
            {
                var method = op.Key.ToString().ToUpperInvariant();
                if (!allowedMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
                    continue;

                var key = $"{method}:{NormalizePath(path)}";
                if (overrides.TryGetValue(key, out var overrideStub))
                {
                    registerStub(overrideStub);
                    count++;
                    continue;
                }

                var preferredStatus = GetPreferredStatusCode(op.Value);
                var stub = _stubFactory.CreateFromOpenApiOperation(path, method, op.Value, document, preferredStatus);
                stub.StatusCode = int.TryParse(preferredStatus, out var sc) ? sc : _openApiConfig?.DefaultSuccessStatusCode ?? 200;
                registerStub(stub);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Generates stubs and returns them as a list (e.g. for later registration).
    /// </summary>
    public IReadOnlyList<StubDefinition> GenerateStubDefinitionsFromSpec(
        string specPath,
        IReadOnlyList<string>? includePaths = null,
        IReadOnlyList<StubDefinition>? overrideStubs = null)
    {
        var list = new List<StubDefinition>();
        GenerateMocksFromSpec(specPath, list.Add, includePaths, overrideStubs);
        return list;
    }

    private static string NormalizePath(string p) => "/" + p.TrimStart('/');
    private static string GetPreferredStatusCode(OpenApiOperation op)
    {
        if (op.Responses?.ContainsKey("200") == true) return "200";
        if (op.Responses?.ContainsKey("201") == true) return "201";
        return op.Responses?.Keys.FirstOrDefault() ?? "200";
    }
}

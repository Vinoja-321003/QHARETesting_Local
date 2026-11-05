using Microsoft.OpenApi.Models;

namespace QualiFrame.Mocking.Generation;

/// <summary>
/// Creates WireMock stub definitions (request matching + response) for registration with WireMock.Net.
/// </summary>
public class StubDefinition
{
    public string Path { get; set; } = "";
    public string Method { get; set; } = "GET";
    public int StatusCode { get; set; } = 200;
    public string? ResponseBody { get; set; }
    public IDictionary<string, string>? ResponseHeaders { get; set; }
    public int? DelayMs { get; set; }
    public string? RequestBodyPattern { get; set; }
    public IDictionary<string, string>? QueryParameters { get; set; }
    public IDictionary<string, string>? PathParameters { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Factory for creating WireMock-compatible stub definitions from OpenAPI and from manual templates.
/// </summary>
public class StubDefinitionFactory
{
    private readonly ResponseTemplateBuilder _responseBuilder;

    public StubDefinitionFactory(ResponseTemplateBuilder? responseBuilder = null)
    {
        _responseBuilder = responseBuilder ?? new ResponseTemplateBuilder();
    }

    /// <summary>
    /// Creates a stub definition from an OpenAPI path and operation.
    /// </summary>
    public StubDefinition CreateFromOpenApiOperation(
        string path,
        string method,
        OpenApiOperation operation,
        OpenApiDocument? document = null,
        string? preferredStatusCode = "200")
    {
        var response = operation.Responses?.GetValueOrDefault(preferredStatusCode ?? "200")
                       ?? operation.Responses?.GetValueOrDefault("default");
        var statusCode = int.TryParse(preferredStatusCode, out var sc) ? sc : 200;
        string? body = null;
        if (response != null)
            body = _responseBuilder.BuildResponseBody(response, preferredStatusCode, document);

        return new StubDefinition
        {
            Path = path,
            Method = method.ToUpperInvariant(),
            StatusCode = statusCode,
            ResponseBody = body,
            ResponseHeaders = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Description = operation.Summary ?? $"{method} {path}"
        };
    }

    /// <summary>
    /// Creates a stub with custom response (for overrides).
    /// </summary>
    public static StubDefinition CreateCustom(
        string path,
        string method,
        int statusCode,
        string? responseBody = null,
        IDictionary<string, string>? headers = null,
        int? delayMs = null,
        string? description = null)
    {
        var h = new Dictionary<string, string>(headers ?? new Dictionary<string, string>());
        if (!h.ContainsKey("Content-Type") && responseBody != null)
            h["Content-Type"] = "application/json";

        return new StubDefinition
        {
            Path = path,
            Method = method.ToUpperInvariant(),
            StatusCode = statusCode,
            ResponseBody = responseBody,
            ResponseHeaders = h,
            DelayMs = delayMs,
            Description = description
        };
    }
}

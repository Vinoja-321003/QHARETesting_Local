namespace QualiFrame.Mocking.Config;

/// <summary>
/// Configuration for OpenAPI/Swagger loading and mock generation.
/// </summary>
public class OpenApiConfig
{
    public const string SectionName = "Mocking:OpenApi";

    /// <summary>Path to the OpenAPI spec file (JSON or YAML).</summary>
    public string SpecPath { get; set; } = "Contracts/swagger.json";

    /// <summary>Base path from the spec to strip or prepend (e.g. /v1).</summary>
    public string? BasePathOverride { get; set; }

    /// <summary>Generate mocks only for these paths (empty = all).</summary>
    public IReadOnlyList<string>? IncludePaths { get; set; }

    /// <summary>Exclude these path patterns from generation.</summary>
    public IReadOnlyList<string>? ExcludePaths { get; set; }

    /// <summary>Prefer example values from the spec when present.</summary>
    public bool PreferExamples { get; set; } = true;

    /// <summary>Default response status when not specified in spec.</summary>
    public int DefaultSuccessStatusCode { get; set; } = 200;

    /// <summary>Generate stubs for these HTTP methods (empty = all standard).</summary>
    public IReadOnlyList<string>? AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
}

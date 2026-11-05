using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace QualiFrame.Mocking.Contracts;

/// <summary>
/// Loads OpenAPI/Swagger specifications from JSON or YAML files or streams.
/// </summary>
public class OpenApiSpecLoader
{
    private readonly OpenApiReaderSettings _readerSettings;

    public OpenApiSpecLoader(OpenApiReaderSettings? readerSettings = null)
    {
        _readerSettings = readerSettings ?? new OpenApiReaderSettings();
    }

    /// <summary>
    /// Loads an OpenAPI document from a file path (supports .json and .yaml/.yml).
    /// </summary>
    public OpenApiLoadResult LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return OpenApiLoadResult.Fail($"File not found: {filePath}");

        using var stream = File.OpenRead(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".json" => LoadFromStream(stream, OpenApiSpecFormat.Json),
            ".yaml" or ".yml" => LoadFromStream(stream, OpenApiSpecFormat.Yaml),
            _ => OpenApiLoadResult.Fail($"Unsupported spec format: {extension}. Use .json or .yaml")
        };
    }

    /// <summary>
    /// Loads an OpenAPI document from a stream.
    /// </summary>
    public OpenApiLoadResult LoadFromStream(Stream stream, OpenApiSpecFormat format = OpenApiSpecFormat.Json)
    {
        var reader = new OpenApiStreamReader(_readerSettings);
        var doc = reader.Read(stream, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
            return OpenApiLoadResult.Fail(string.Join("; ", diagnostic.Errors.Select(e => e.Message)), diagnostic);

        return doc != null
            ? OpenApiLoadResult.Success(doc)
            : OpenApiLoadResult.Fail("OpenAPI document was null after read.", diagnostic);
    }

    /// <summary>
    /// Loads from a string (JSON or YAML content).
    /// </summary>
    public OpenApiLoadResult LoadFromString(string content, OpenApiSpecFormat format = OpenApiSpecFormat.Json)
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return LoadFromStream(ms, format);
    }
}

/// <summary>
/// Result of loading an OpenAPI spec.
/// </summary>
public class OpenApiLoadResult
{
    public bool IsSuccess { get; private init; }
    public OpenApiDocument? Document { get; private init; }
    public string? ErrorMessage { get; private init; }
    public OpenApiDiagnostic? Diagnostic { get; private init; }

    public static OpenApiLoadResult Success(OpenApiDocument document) => new()
    {
        IsSuccess = true,
        Document = document
    };

    public static OpenApiLoadResult Fail(string message, OpenApiDiagnostic? diagnostic = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        Diagnostic = diagnostic
    };
}

public enum OpenApiSpecFormat
{
    Json,
    Yaml
}

using Microsoft.OpenApi.Models;

namespace QualiFrame.Mocking.Contracts;

/// <summary>
/// Validates that API responses (and optionally requests) conform to the OpenAPI contract.
/// Coordinates status code validation and schema validation.
/// </summary>
public class ContractValidator
{
    private readonly SchemaValidationService _schemaValidation;
    private readonly OpenApiSpecLoader _loader;

    public ContractValidator(SchemaValidationService? schemaValidation = null, OpenApiSpecLoader? loader = null)
    {
        _schemaValidation = schemaValidation ?? new SchemaValidationService();
        _loader = loader ?? new OpenApiSpecLoader();
    }

    /// <summary>
    /// Validates that the response status code is declared in the OpenAPI spec for the operation.
    /// </summary>
    public ContractValidationResult ValidateStatusCode(
        OpenApiDocument document,
        string path,
        string method,
        int statusCode)
    {
        var operation = GetOperation(document, path, method);
        if (operation?.Responses == null)
            return ContractValidationResult.Fail($"No responses defined for {method} {path}");

        var codeKey = statusCode.ToString();
        if (operation.Responses.ContainsKey(codeKey) || operation.Responses.ContainsKey("default"))
            return ContractValidationResult.Success();

        return ContractValidationResult.Fail(
            $"Status code {statusCode} is not declared in the OpenAPI spec for {method} {path}. " +
            $"Declared: {string.Join(", ", operation.Responses.Keys)}");
    }

    /// <summary>
    /// Validates response body against the operation's response schema.
    /// </summary>
    public SchemaValidationResult ValidateResponseBody(
        string jsonBody,
        OpenApiDocument document,
        string path,
        string method,
        string? statusCode = "200")
    {
        return _schemaValidation.ValidateResponseBody(jsonBody, document, path, method, statusCode);
    }

    /// <summary>
    /// Full contract validation: status code + response body schema.
    /// </summary>
    public ContractValidationResult ValidateResponse(
        OpenApiDocument document,
        string path,
        string method,
        int statusCode,
        string? responseBody)
    {
        var statusResult = ValidateStatusCode(document, path, method, statusCode);
        if (!statusResult.IsValid)
            return statusResult;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            var schemaResult = _schemaValidation.ValidateResponseBody(
                responseBody, document, path, method, statusCode.ToString());
            if (!schemaResult.IsValid)
                return ContractValidationResult.Fail($"Schema validation failed: {schemaResult.Message}", schemaResult.Errors?.Select(e => new ContractError(e.Path, e.Kind)).ToList());
        }

        return ContractValidationResult.Success();
    }

    /// <summary>
    /// Loads spec from file and validates the response.
    /// </summary>
    public ContractValidationResult ValidateResponseAgainstSpec(
        string specPath,
        string path,
        string method,
        int statusCode,
        string? responseBody)
    {
        var load = _loader.LoadFromFile(specPath);
        if (!load.IsSuccess || load.Document == null)
            return ContractValidationResult.Fail($"Failed to load spec: {load.ErrorMessage}");

        return ValidateResponse(load.Document, path, method, statusCode, responseBody);
    }

    private static OpenApiOperation? GetOperation(OpenApiDocument document, string path, string method)
    {
        var pathKey = path.TrimStart('/');
        if (!document.Paths.TryGetValue(pathKey, out var pathItem))
            pathItem = document.Paths.FirstOrDefault(p => p.Key.TrimStart('/') == pathKey).Value;
        if (pathItem == null) return null;

        var m = method.ToUpperInvariant();
        if (m == "GET" && pathItem.Operations.TryGetValue(OperationType.Get, out var getOp)) return getOp;
        if (m == "POST" && pathItem.Operations.TryGetValue(OperationType.Post, out var postOp)) return postOp;
        if (m == "PUT" && pathItem.Operations.TryGetValue(OperationType.Put, out var putOp)) return putOp;
        if (m == "PATCH" && pathItem.Operations.TryGetValue(OperationType.Patch, out var patchOp)) return patchOp;
        if (m == "DELETE" && pathItem.Operations.TryGetValue(OperationType.Delete, out var delOp)) return delOp;
        return null;
    }
}

/// <summary>
/// Result of contract validation.
/// </summary>
public class ContractValidationResult
{
    public bool IsValid { get; private init; }
    public string? Message { get; private init; }
    public IReadOnlyList<ContractError>? Errors { get; private init; }

    public static ContractValidationResult Success() => new() { IsValid = true };
    public static ContractValidationResult Fail(string message, IReadOnlyList<ContractError>? errors = null) => new()
    {
        IsValid = false,
        Message = message,
        Errors = errors
    };
}

public record ContractError(string Path, string Kind);

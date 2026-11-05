using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;

namespace QualiFrame.Mocking.Contracts;

/// <summary>
/// Validates JSON response/request bodies against OpenAPI schema definitions.
/// </summary>
public class SchemaValidationService
{
    /// <summary>
    /// Validates a JSON body against the schema referenced by the given operation response.
    /// </summary>
    public SchemaValidationResult ValidateResponseBody(
        string jsonBody,
        OpenApiDocument document,
        string path,
        string operationMethod,
        string? responseStatusCode = "200")
    {
        var operation = GetOperation(document, path, operationMethod);
        if (operation?.Responses == null)
            return SchemaValidationResult.Fail($"No responses found for {operationMethod} {path}");

        if (!operation.Responses.TryGetValue(responseStatusCode ?? "200", out var response))
            response = operation.Responses.TryGetValue("default", out var def) ? def : null;

        if (response?.Content == null || !response.Content.Any())
            return SchemaValidationResult.Success(); // No schema to validate

        var mediaType = response.Content.TryGetValue("application/json", out var jsonMt) ? jsonMt : response.Content.Values.FirstOrDefault();
        if (mediaType?.Schema == null)
            return SchemaValidationResult.Success();

        return ValidateAgainstOpenApiSchema(jsonBody, mediaType.Schema, document);
    }

    /// <summary>
    /// Validates a JSON body against an OpenAPI schema (required fields and types).
    /// </summary>
    public SchemaValidationResult ValidateAgainstOpenApiSchema(
        string jsonBody,
        OpenApiSchema schema,
        OpenApiDocument? document = null)
    {
        var errors = new List<SchemaError>();
        try
        {
            var token = string.IsNullOrWhiteSpace(jsonBody) ? null : JToken.Parse(jsonBody);
            ValidateRequiredAndTypes(schema, token, "", document?.Components?.Schemas, errors);
            if (errors.Count == 0)
                return SchemaValidationResult.Success();
            return SchemaValidationResult.Fail(string.Join("; ", errors.Select(e => $"{e.Path}: {e.Kind}")), errors);
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Fail($"Validation threw: {ex.Message}", new List<SchemaError> { new("", ex.Message) });
        }
    }

    private static void ValidateRequiredAndTypes(
        OpenApiSchema schema,
        JToken? value,
        string path,
        IDictionary<string, OpenApiSchema>? components,
        List<SchemaError> errors)
    {
        if (schema.Reference?.Id != null && components != null && components.TryGetValue(schema.Reference.Id, out var resolved))
        {
            ValidateRequiredAndTypes(resolved, value, path, components, errors);
            return;
        }

        if (schema.Required != null)
        {
            var obj = value as JObject;
            if (IsObjectSchema(schema) && obj == null && value != null)
            {
                errors.Add(new SchemaError(path, "Expected object"));
                return;
            }
            foreach (var req in schema.Required)
            {
                var prop = obj?[req];
                if (prop == null || prop.Type == JTokenType.Null || prop.Type == JTokenType.Undefined)
                    errors.Add(new SchemaError(path + (path.Length > 0 ? "." : "") + req, "Required field missing"));
            }
        }

        if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
            return;

        if (IsObjectSchema(schema))
        {
            var jObj = value as JObject;
            if (jObj == null) { errors.Add(new SchemaError(path, "Expected object")); return; }
            if (schema.Properties != null)
            {
                foreach (var prop in schema.Properties)
                {
                    var child = jObj[prop.Key];
                    if (child != null && prop.Value != null)
                        ValidateRequiredAndTypes(prop.Value, child, path + (path.Length > 0 ? "." : "") + prop.Key, components, errors);
                }
            }
        }
        else if (IsArraySchema(schema))
        {
            if (value is not JArray)
                errors.Add(new SchemaError(path, "Expected array"));
            else if (schema.Items != null)
            {
                var arr = (JArray)value;
                for (int i = 0; i < arr.Count; i++)
                    ValidateRequiredAndTypes(schema.Items, arr[i], $"{path}[{i}]", components, errors);
            }
        }
        else if (IsStringSchema(schema))
        {
            if (value.Type != JTokenType.String) errors.Add(new SchemaError(path, "Expected string"));
        }
        else if (IsIntegerSchema(schema))
        {
            if (value.Type != JTokenType.Integer) errors.Add(new SchemaError(path, "Expected integer"));
        }
        else if (IsNumberSchema(schema))
        {
            if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float) errors.Add(new SchemaError(path, "Expected number"));
        }
        else if (IsBooleanSchema(schema))
        {
            if (value.Type != JTokenType.Boolean) errors.Add(new SchemaError(path, "Expected boolean"));
        }
    }

    private static bool IsObjectSchema(OpenApiSchema schema) => IsSchemaType(schema, "Object");
    private static bool IsArraySchema(OpenApiSchema schema) => IsSchemaType(schema, "Array");
    private static bool IsStringSchema(OpenApiSchema schema) => IsSchemaType(schema, "String");
    private static bool IsIntegerSchema(OpenApiSchema schema) => IsSchemaType(schema, "Integer");
    private static bool IsNumberSchema(OpenApiSchema schema) => IsSchemaType(schema, "Number");
    private static bool IsBooleanSchema(OpenApiSchema schema) => IsSchemaType(schema, "Boolean");

    private static bool IsSchemaType(OpenApiSchema schema, string typeName)
    {
        var t = schema.Type?.ToString();
        return string.Equals(t, typeName, StringComparison.OrdinalIgnoreCase);
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
/// Result of schema validation.
/// </summary>
public class SchemaValidationResult
{
    public bool IsValid { get; private init; }
    public string? Message { get; private init; }
    public IReadOnlyList<SchemaError>? Errors { get; private init; }

    public static SchemaValidationResult Success() => new() { IsValid = true };
    public static SchemaValidationResult Fail(string message, IReadOnlyList<SchemaError>? errors = null) => new()
    {
        IsValid = false,
        Message = message,
        Errors = errors
    };
}

public record SchemaError(string Path, string Kind);

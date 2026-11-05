using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;

namespace QualiFrame.Mocking.Generation;

/// <summary>
/// Builds response body JSON from OpenAPI response schemas, using examples when present or schema-based defaults.
/// </summary>
public class ResponseTemplateBuilder
{
    private readonly bool _preferExamples;

    public ResponseTemplateBuilder(bool preferExamples = true)
    {
        _preferExamples = preferExamples;
    }

    /// <summary>
    /// Builds a response body for the given status code from the OpenAPI response definition.
    /// </summary>
    public string? BuildResponseBody(OpenApiResponse response, string? statusCode = "200", OpenApiDocument? document = null)
    {
        var content = response.Content != null && response.Content.TryGetValue("application/json", out var jsonContent)
            ? jsonContent
            : response.Content?.Values.FirstOrDefault();
        if (content?.Schema == null)
            return null;

        if (_preferExamples && content.Example != null)
            return content.Example.ToString();

        if (_preferExamples && content.Examples != null && content.Examples.Any())
        {
            var first = content.Examples.Values.FirstOrDefault();
            if (first?.Value != null)
                return first.Value.ToString();
        }

        return BuildFromSchema(content.Schema, document?.Components?.Schemas);
    }

    /// <summary>
    /// Generates a default JSON value from an OpenAPI schema.
    /// </summary>
    public string? BuildFromSchema(OpenApiSchema schema, IDictionary<string, OpenApiSchema>? components = null)
    {
        var token = BuildToken(schema, components);
        return token?.ToString();
    }

    private JToken? BuildToken(OpenApiSchema schema, IDictionary<string, OpenApiSchema>? components)
    {
        if (schema.Reference?.Id != null && components != null && components.TryGetValue(schema.Reference.Id, out var resolved))
            return BuildToken(resolved, components);

        if (schema.Example != null)
            return JToken.FromObject(schema.Example);

        if (IsStringSchema(schema))
            return schema.Default != null ? JToken.FromObject(schema.Default) : "";
        if (IsIntegerSchema(schema))
            return schema.Default != null ? JToken.FromObject(schema.Default) : 0;
        if (IsNumberSchema(schema))
            return schema.Default != null ? JToken.FromObject(schema.Default) : 0.0;
        if (IsBooleanSchema(schema))
            return schema.Default != null ? JToken.FromObject(schema.Default) : false;
        if (IsArraySchema(schema))
        {
            var arr = new JArray();
            if (schema.Items != null)
            {
                var item = BuildToken(schema.Items, components);
                if (item != null) arr.Add(item);
            }
            return arr;
        }
        if (IsObjectSchema(schema))
        {
            var obj = new JObject();
            if (schema.Properties != null)
            {
                foreach (var prop in schema.Properties)
                {
                    var value = BuildToken(prop.Value, components);
                    if (value != null)
                        obj[prop.Key] = value;
                }
            }
            return obj;
        }
        return null;
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
}

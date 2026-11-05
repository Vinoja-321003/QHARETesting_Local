using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API
{
    /// <summary>
    /// Validates JSON responses against schemas.
    /// </summary>
    public static class SchemaValidator
    {
        /// <summary>
        /// Validates JSON response against a schema.
        /// </summary>
        public static bool Validate(string jsonResponse, JObject schema, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                var response = JObject.Parse(jsonResponse);
                ValidateObject(response, schema, "", errors);
                
                if (errors.Count > 0)
                {
                    Logger.Warning("Schema validation failed with {ErrorCount} errors", errors.Count);
                    return false;
                }

                Logger.Debug("Schema validation passed");
                return true;
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid JSON format: {ex.Message}");
                Logger.Error(ex, "Invalid JSON format: {Error}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                Logger.Error(ex, "Schema validation error: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validates a simple response structure (checks for required fields).
        /// </summary>
        public static bool ValidateRequiredFields(string jsonResponse, params string[] requiredFields)
        {
            try
            {
                var response = JObject.Parse(jsonResponse);
                var missingFields = new List<string>();

                foreach (var field in requiredFields)
                {
                    if (response[field] == null)
                    {
                        missingFields.Add(field);
                    }
                }

                if (missingFields.Count > 0)
                {
                    Logger.Warning("Missing required fields: {Fields}", string.Join(", ", missingFields));
                    return false;
                }

                Logger.Debug("All required fields present");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error validating required fields: {Error}", ex.Message);
                return false;
            }
        }

        private static void ValidateObject(JToken response, JObject schema, string path, List<string> errors)
        {
            if (schema["type"]?.ToString() == "object")
            {
                var properties = schema["properties"] as JObject;
                if (properties != null)
                {
                    foreach (var property in properties.Properties())
                    {
                        var propertyName = property.Name;
                        var propertySchema = property.Value as JObject;
                        var currentPath = string.IsNullOrEmpty(path) ? propertyName : $"{path}.{propertyName}";

                        if (response[propertyName] == null)
                        {
                            var required = schema["required"] as JArray;
                            if (required != null && required.ToString().Contains(propertyName))
                            {
                                errors.Add($"Missing required field: {currentPath}");
                            }
                        }
                        else if (propertySchema != null && response[propertyName] != null)
                        {
                            ValidateProperty(response[propertyName]!, propertySchema, currentPath, errors);
                        }
                    }
                }
            }
        }

        private static void ValidateProperty(JToken value, JObject schema, string path, List<string> errors)
        {
            var type = schema["type"]?.ToString();

            switch (type)
            {
                case "string":
                    if (value.Type != JTokenType.String)
                    {
                        errors.Add($"Field {path} should be a string");
                    }
                    break;

                case "integer":
                    if (value.Type != JTokenType.Integer)
                    {
                        errors.Add($"Field {path} should be an integer");
                    }
                    break;

                case "number":
                    if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)
                    {
                        errors.Add($"Field {path} should be a number");
                    }
                    break;

                case "boolean":
                    if (value.Type != JTokenType.Boolean)
                    {
                        errors.Add($"Field {path} should be a boolean");
                    }
                    break;

                case "array":
                    if (value.Type != JTokenType.Array)
                    {
                        errors.Add($"Field {path} should be an array");
                    }
                    else
                    {
                        var itemsSchema = schema["items"] as JObject;
                        if (itemsSchema != null)
                        {
                            var array = value as JArray;
                            if (array != null)
                            {
                                for (int i = 0; i < array.Count; i++)
                                {
                                    ValidateProperty(array[i], itemsSchema, $"{path}[{i}]", errors);
                                }
                            }
                        }
                    }
                    break;

                case "object":
                    if (value.Type != JTokenType.Object)
                    {
                        errors.Add($"Field {path} should be an object");
                    }
                    else
                    {
                        var objValue = value as JObject;
                        if (objValue != null)
                        {
                            ValidateObject(objValue, schema, path, errors);
                        }
                    }
                    break;
            }
        }
    }
}

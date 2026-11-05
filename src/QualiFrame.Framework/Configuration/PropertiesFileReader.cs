using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Configuration
{
    /// <summary>
    /// Reads Java-style properties files (key=value format).
    /// Supports extent.properties and other properties files similar to Java framework.
    /// </summary>
    public class PropertiesFileReader
    {
        private readonly Dictionary<string, string> _properties;

        private PropertiesFileReader(Dictionary<string, string> properties)
        {
            _properties = properties;
        }

        /// <summary>
        /// Loads a properties file from the specified path.
        /// </summary>
        public static PropertiesFileReader LoadFromFile(string filePath)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(filePath))
            {
                Logger.Warning("Properties file not found: {Path}. Using defaults.", filePath);
                return new PropertiesFileReader(properties);
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                
                foreach (var line in lines)
                {
                    // Skip empty lines and comments
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    // Parse key=value pairs
                    var equalIndex = trimmedLine.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        var key = trimmedLine.Substring(0, equalIndex).Trim();
                        var value = trimmedLine.Substring(equalIndex + 1).Trim();
                        
                        // Remove quotes if present
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        
                        properties[key] = value;
                    }
                }

                Logger.Information("Loaded {Count} properties from: {Path}", properties.Count, filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load properties file: {Path}", filePath);
            }

            return new PropertiesFileReader(properties);
        }

        /// <summary>
        /// Gets a property value by key, with optional default value.
        /// </summary>
        public string GetProperty(string key, string? defaultValue = null)
        {
            return _properties.TryGetValue(key, out var value) ? value : (defaultValue ?? string.Empty);
        }

        /// <summary>
        /// Gets a boolean property value.
        /// </summary>
        public bool GetBooleanProperty(string key, bool defaultValue = false)
        {
            var value = GetProperty(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return bool.TryParse(value, out var result) && result;
        }

        /// <summary>
        /// Gets an integer property value.
        /// </summary>
        public int GetIntProperty(string key, int defaultValue = 0)
        {
            var value = GetProperty(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Checks if a property exists.
        /// </summary>
        public bool HasProperty(string key)
        {
            return _properties.ContainsKey(key);
        }

        /// <summary>
        /// Gets all properties as a dictionary.
        /// </summary>
        public Dictionary<string, string> GetAllProperties()
        {
            return new Dictionary<string, string>(_properties);
        }
    }
}

using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Base class for keyword implementations.
    /// </summary>
    public abstract class BaseKeyword : IKeyword
    {
        /// <summary>
        /// Gets the keyword name.
        /// </summary>
        public abstract string KeywordName { get; }

        /// <summary>
        /// Executes the keyword action.
        /// </summary>
        public KeywordResult Execute(IWebDriver driver, Dictionary<string, object> parameters)
        {
            try
            {
                Logger.Debug("Executing keyword: {KeywordName} with parameters: {Parameters}", 
                    KeywordName, string.Join(", ", parameters.Keys));
                
                var result = ExecuteKeyword(driver, parameters);
                
                if (result.Success)
                {
                    Logger.Information("Keyword executed successfully: {KeywordName}", KeywordName);
                }
                else
                {
                    Logger.Warning("Keyword execution failed: {KeywordName}, Message: {Message}", 
                        KeywordName, result.Message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing keyword: {KeywordName}", KeywordName);
                return KeywordResult.Fail($"Error executing keyword {KeywordName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes the keyword-specific logic.
        /// </summary>
        protected abstract KeywordResult ExecuteKeyword(IWebDriver driver, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets a required parameter value.
        /// </summary>
        protected T GetRequiredParameter<T>(Dictionary<string, object> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out var value))
            {
                throw new ArgumentException($"Required parameter '{key}' is missing for keyword '{KeywordName}'");
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                throw new ArgumentException($"Parameter '{key}' cannot be converted to {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Gets an optional parameter value.
        /// </summary>
        protected T? GetOptionalParameter<T>(Dictionary<string, object> parameters, string key, T? defaultValue = default)
        {
            if (!parameters.TryGetValue(key, out var value))
            {
                return defaultValue;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

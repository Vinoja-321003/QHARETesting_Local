using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Registry for keyword actions in keyword-driven testing.
    /// </summary>
    public class KeywordRegistry
    {
        private readonly Dictionary<string, IKeyword> _keywords = new();
        private static KeywordRegistry? _instance;
        private static readonly object _lock = new object();

        private KeywordRegistry()
        {
            RegisterDefaultKeywords();
        }

        /// <summary>
        /// Gets the singleton instance of KeywordRegistry.
        /// </summary>
        public static KeywordRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new KeywordRegistry();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Registers a keyword.
        /// </summary>
        public void RegisterKeyword(IKeyword keyword)
        {
            if (keyword == null)
            {
                throw new ArgumentNullException(nameof(keyword));
            }

            var keywordName = keyword.KeywordName.ToUpperInvariant();
            if (_keywords.ContainsKey(keywordName))
            {
                Logger.Warning("Keyword '{KeywordName}' is already registered. Overwriting.", keywordName);
            }

            _keywords[keywordName] = keyword;
            Logger.Debug("Registered keyword: {KeywordName}", keywordName);
        }

        /// <summary>
        /// Gets a keyword by name.
        /// </summary>
        public IKeyword? GetKeyword(string keywordName)
        {
            var upperName = keywordName.ToUpperInvariant();
            return _keywords.TryGetValue(upperName, out var keyword) ? keyword : null;
        }

        /// <summary>
        /// Gets all registered keywords.
        /// </summary>
        public IEnumerable<string> GetAllKeywords()
        {
            return _keywords.Keys;
        }

        /// <summary>
        /// Checks if a keyword is registered.
        /// </summary>
        public bool IsRegistered(string keywordName)
        {
            return _keywords.ContainsKey(keywordName.ToUpperInvariant());
        }

        /// <summary>
        /// Registers default keywords from the Keywords namespace.
        /// </summary>
        private void RegisterDefaultKeywords()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var keywordTypes = assembly.GetTypes()
                    .Where(t => typeof(IKeyword).IsAssignableFrom(t) 
                                && !t.IsInterface 
                                && !t.IsAbstract
                                && t.Namespace?.Contains("KeywordDriven.Keywords") == true);

                foreach (var type in keywordTypes)
                {
                    try
                    {
                        var keyword = (IKeyword)Activator.CreateInstance(type)!;
                        RegisterKeyword(keyword);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Failed to register keyword type {Type}: {Error}", type.Name, ex.Message);
                    }
                }

                Logger.Information("Registered {Count} default keywords", _keywords.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error registering default keywords");
            }
        }
    }
}

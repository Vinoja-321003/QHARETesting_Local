using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// Official Healenium HTTP client: save selectors, load references, persist healing, init reports.
    /// </summary>
    public sealed class HealeniumClient : IDisposable
    {
        private readonly HealeniumConfiguration _config;
        private readonly HttpClient _backend;
        private readonly HttpClient _imitator;
        private readonly bool _ownsBackend;
        private readonly bool _ownsImitator;
        private readonly JsonSerializerOptions _json;
        private bool _disposed;

        public HealeniumClient(HealeniumConfiguration config, HttpClient? backendClient = null, HttpClient? imitatorClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _json = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            if (backendClient != null)
            {
                _backend = backendClient;
            }
            else
            {
                _ownsBackend = true;
                _backend = new HttpClient
                {
                    BaseAddress = NormalizeBase(config.BackendUrl),
                    Timeout = TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds))
                };
                _backend.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }

            if (imitatorClient != null)
            {
                _imitator = imitatorClient;
            }
            else
            {
                _ownsImitator = true;
                _imitator = new HttpClient
                {
                    BaseAddress = NormalizeBase(config.ImitatorUrl),
                    Timeout = TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds))
                };
                _imitator.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }
        }

        public string InitReport(string sessionId)
        {
            using var response = _backend.PostAsync($"healenium/report/init/{Uri.EscapeDataString(sessionId)}", null).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("Healenium report init failed ({Status}): {Body}", (int)response.StatusCode, body);
                return string.Empty;
            }

            if (_config.LogHealingAttempts)
            {
                Logger.Information("Healenium report initialized: {Path}", body);
            }

            return body;
        }

        public void SaveSelector(HealeniumSelectorRequest request)
        {
            using var response = _backend.PostAsJsonAsync("healenium", request, _json).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Logger.Warning("Healenium save selector failed ({Status}): {Body}", (int)response.StatusCode, body);
            }
        }

        public HealeniumReferenceElements? GetReferenceElements(string locator, string className, string methodName, string command, string url)
        {
            var query = $"healenium?locator={Uri.EscapeDataString(locator)}" +
                        $"&className={Uri.EscapeDataString(className)}" +
                        $"&methodName={Uri.EscapeDataString(methodName)}" +
                        $"&command={Uri.EscapeDataString(command)}" +
                        $"&url={Uri.EscapeDataString(url ?? string.Empty)}";

            var response = _backend.GetAsync(query).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("Healenium get reference failed ({Status})", (int)response.StatusCode);
                return null;
            }

            return response.Content.ReadFromJsonAsync<HealeniumReferenceElements>(_json).GetAwaiter().GetResult();
        }

        public List<HealeniumStoredSelector> GetAllSelectors()
        {
            try
            {
                using var response = _backend.GetAsync("healenium/selector/all").GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning("Healenium get all selectors failed ({Status})", (int)response.StatusCode);
                    return new List<HealeniumStoredSelector>();
                }

                return response.Content.ReadFromJsonAsync<List<HealeniumStoredSelector>>(_json).GetAwaiter().GetResult()
                       ?? new List<HealeniumStoredSelector>();
            }
            catch (Exception ex)
            {
                Logger.Debug("Healenium get all selectors skipped: {Message}", ex.Message);
                return new List<HealeniumStoredSelector>();
            }
        }

        public bool SaveHealing(HealeniumHealingRequest request, string sessionId)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "healenium/healing")
            {
                Content = JsonContent.Create(new[] { request }, options: _json)
            };
            message.Headers.TryAddWithoutValidation("sessionKey", sessionId);
            message.Headers.TryAddWithoutValidation("hostProject", "QualiFrame");

            using var response = _backend.SendAsync(message).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Logger.Warning("Healenium save healing failed ({Status}): {Body}", (int)response.StatusCode, body);
                return false;
            }

            return true;
        }

        public List<HealeniumLocatorDto> Imitate(HealeniumLocatorDto userSelector, HealeniumNode targetNode)
        {
            try
            {
                var payload = new HealeniumImitateRequest
                {
                    UserSelector = userSelector,
                    TargetNode = targetNode
                };
                var response = _imitator.PostAsJsonAsync("imitate", payload, _json).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    return new List<HealeniumLocatorDto>();
                }

                return response.Content.ReadFromJsonAsync<List<HealeniumLocatorDto>>(_json).GetAwaiter().GetResult()
                       ?? new List<HealeniumLocatorDto>();
            }
            catch (Exception ex)
            {
                Logger.Debug("Selector imitator call failed: {Message}", ex.Message);
                return new List<HealeniumLocatorDto>();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_ownsBackend)
            {
                _backend.Dispose();
            }

            if (_ownsImitator)
            {
                _imitator.Dispose();
            }

            _disposed = true;
        }

        private static Uri NormalizeBase(string url)
        {
            var trimmed = string.IsNullOrWhiteSpace(url) ? "http://localhost:7878" : url.Trim();
            if (!trimmed.EndsWith('/'))
            {
                trimmed += "/";
            }

            return new Uri(trimmed);
        }
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Utilities.WebDriver
{
    /// <summary>
    /// HTTP client for QHARE core: heal, recover, manifest, and telemetry.
    /// </summary>
    public sealed class QhareClient : IDisposable
    {
        public const string AdapterVersion = "0.1.0";
        public const int MaxDomChars = 250_000;

        private readonly QhareConfiguration _config;
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;
        private readonly JsonSerializerOptions _json;
        private QhareManifest? _manifest;
        private bool _disposed;

        public QhareClient(QhareConfiguration config, HttpClient? httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _json = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            if (httpClient != null)
            {
                _http = httpClient;
                _ownsHttp = false;
            }
            else
            {
                _ownsHttp = true;
                _http = new HttpClient
                {
                    BaseAddress = NormalizeBase(config.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds))
                };
                _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
                }
            }
        }

        public string ProjectId => _config.ProjectId;
        public IReadOnlyList<string> LocatorStrategies =>
            (IReadOnlyList<string>?)_manifest?.LocatorStrategies ?? Array.Empty<string>();

        public QhareHealResponse Heal(object payload)
        {
            return PostJson<QhareHealResponse>("api/v1/heal", payload)
                   ?? new QhareHealResponse { Status = "FAILED", Reason = "Empty QHARE heal response." };
        }

        public QhareRecoverResponse Recover(object payload)
        {
            return PostJson<QhareRecoverResponse>("api/v1/recover", payload)
                   ?? new QhareRecoverResponse { Status = "FAILED" };
        }

        public void EnsureManifest()
        {
            if (_manifest != null)
            {
                return;
            }

            try
            {
                var path = $"api/v1/projects/{Uri.EscapeDataString(ProjectId)}/manifest?framework=selenium";
                using var response = _http.GetAsync(path).GetAwaiter().GetResult();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning("QHARE manifest load failed ({Status}): {Body}", (int)response.StatusCode, body);
                    _manifest = new QhareManifest();
                    return;
                }

                _manifest = JsonSerializer.Deserialize<QhareManifest>(body, _json) ?? new QhareManifest();
            }
            catch (Exception ex)
            {
                Logger.Debug("QHARE manifest skipped: {Message}", ex.Message);
                _manifest = new QhareManifest();
            }
        }

        public bool Emit(string type, string executionId, string testId, object payload)
        {
            try
            {
                var body = new
                {
                    type,
                    projectId = ProjectId,
                    executionId,
                    testId,
                    payload
                };
                using var response = _http.PostAsJsonAsync("api/v1/telemetry/events", body, _json).GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Debug("QHARE telemetry skipped: {Message}", ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_ownsHttp)
            {
                _http.Dispose();
            }

            _disposed = true;
        }

        private T? PostJson<T>(string path, object payload)
        {
            using var response = _http.PostAsJsonAsync(path, payload, _json).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"QHARE {path} failed: {(int)response.StatusCode}");
            }

            return JsonSerializer.Deserialize<T>(body, _json);
        }

        internal static Uri NormalizeBase(string url)
        {
            var trimmed = string.IsNullOrWhiteSpace(url) ? "http://localhost:8000" : url.Trim();
            if (!trimmed.EndsWith('/'))
            {
                trimmed += "/";
            }

            return new Uri(trimmed);
        }

        internal static string TruncateDom(string? pageSource)
        {
            if (string.IsNullOrEmpty(pageSource))
            {
                return string.Empty;
            }

            return pageSource.Length <= MaxDomChars ? pageSource : pageSource[..MaxDomChars];
        }
    }
}

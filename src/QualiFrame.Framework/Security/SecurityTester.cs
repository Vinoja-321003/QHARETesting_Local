using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Security
{
    /// <summary>
    /// Security testing using OWASP ZAP for vulnerability scanning.
    /// </summary>
    public class SecurityTester : IDisposable
    {
        private readonly HttpClient? _httpClient;
        private readonly TestConfiguration _config;
        private readonly string? _zapApiUrl;
        private readonly string? _zapApiKey;
        private bool _disposed = false;

        public SecurityTester()
        {
            _config = TestConfiguration.Instance;
            (_zapApiUrl, _zapApiKey, _httpClient) = InitializeZapClient();
        }

        /// <summary>
        /// True when OWASP ZAP is enabled and the API URL answers.
        /// Tests should call this and mark inconclusive instead of failing locally/CI.
        /// </summary>
        public bool IsAvailable()
        {
            if (!_config.OWASPZAPEnabled || _httpClient == null || string.IsNullOrWhiteSpace(_zapApiUrl))
            {
                return false;
            }

            try
            {
                var apiKeyParam = string.IsNullOrEmpty(_zapApiKey) ? "" : $"?apikey={_zapApiKey}";
                var response = _httpClient.GetAsync($"{_zapApiUrl}/JSON/core/view/version/{apiKeyParam}").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Debug("OWASP ZAP is not reachable: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Runs a security scan on the specified URL.
        /// </summary>
        public async Task<SecurityScanResults> RunSecurityScan(string url, bool runSpider = true, bool runActiveScan = true)
        {
            if (_httpClient == null || string.IsNullOrEmpty(_zapApiUrl))
            {
                throw new InvalidOperationException("OWASP ZAP client is not initialized. Check your configuration.");
            }

            try
            {
                Logger.Information("Starting security scan for URL: {Url}", url);

                var results = new SecurityScanResults
                {
                    Url = url,
                    StartTime = DateTime.UtcNow
                };

                if (runSpider)
                {
                    Logger.Information("Running spider scan...");
                    var spiderScanId = await RunSpiderScanAsync(url);
                    await WaitForScanCompletionAsync(spiderScanId, "spider");
                    results.SpiderScanCompleted = true;
                }

                if (runActiveScan)
                {
                    Logger.Information("Running active scan...");
                    var activeScanId = await RunActiveScanAsync(url);
                    await WaitForScanCompletionAsync(activeScanId, "active");
                    results.ActiveScanCompleted = true;
                }

                results.Alerts = await GetAlertsAsync();
                results.Vulnerabilities = CategorizeVulnerabilities(results.Alerts);
                results.EndTime = DateTime.UtcNow;
                results.Duration = results.EndTime - results.StartTime;

                Logger.Information("Security scan completed. Found {Count} alerts, {HighRisk} high risk vulnerabilities",
                    results.Alerts.Count, results.Vulnerabilities.HighRisk);

                return results;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to run security scan for URL: {Url}", url);
                throw;
            }
        }

        /// <summary>
        /// Gets security alerts from ZAP.
        /// </summary>
        public async Task<List<SecurityAlert>> GetAlertsAsync()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_zapApiUrl))
            {
                return new List<SecurityAlert>();
            }

            try
            {
                var apiKeyParam = string.IsNullOrEmpty(_zapApiKey) ? "" : $"?apikey={_zapApiKey}";
                var response = await _httpClient.GetStringAsync($"{_zapApiUrl}/JSON/core/view/alerts/{apiKeyParam}");
                
                var jsonDoc = JsonDocument.Parse(response);
                var alerts = new List<SecurityAlert>();

                if (jsonDoc.RootElement.TryGetProperty("alerts", out var alertsArray))
                {
                    foreach (var alert in alertsArray.EnumerateArray())
                    {
                        alerts.Add(new SecurityAlert
                        {
                            Name = alert.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                            Risk = alert.TryGetProperty("risk", out var risk) ? risk.GetString() ?? string.Empty : string.Empty,
                            Confidence = alert.TryGetProperty("confidence", out var conf) ? conf.GetString() ?? string.Empty : string.Empty,
                            Description = alert.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                            Solution = alert.TryGetProperty("solution", out var sol) ? sol.GetString() ?? string.Empty : string.Empty,
                            Url = alert.TryGetProperty("url", out var url) ? url.GetString() ?? string.Empty : string.Empty
                        });
                    }
                }

                Logger.Debug("Retrieved {Count} security alerts from ZAP", alerts.Count);
                return alerts;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get security alerts");
                return new List<SecurityAlert>();
            }
        }

        /// <summary>
        /// Generates a security report.
        /// </summary>
        public async Task<string> GenerateReportAsync(SecurityScanResults results, string format = "HTML")
        {
            if (_httpClient == null || string.IsNullOrEmpty(_zapApiUrl))
            {
                throw new InvalidOperationException("OWASP ZAP client is not initialized.");
            }

            try
            {
                var reportPath = Path.Combine(_config.OWASPZAPReportPath, 
                    $"security-report-{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToLower()}");

                Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

                string reportContent;

                try
                {
                    var apiKeyParam = string.IsNullOrEmpty(_zapApiKey) ? "" : $"?apikey={_zapApiKey}";
                    var reportUrl = format.ToUpper() switch
                    {
                        "HTML" => $"{_zapApiUrl}/OTHER/core/other/htmlreport/{apiKeyParam}",
                        "JSON" => $"{_zapApiUrl}/JSON/core/view/alerts/{apiKeyParam}",
                        "XML" => $"{_zapApiUrl}/OTHER/core/other/xmlreport/{apiKeyParam}",
                        _ => $"{_zapApiUrl}/OTHER/core/other/htmlreport/{apiKeyParam}"
                    };

                    reportContent = await _httpClient.GetStringAsync(reportUrl);
                }
                catch
                {
                    reportContent = GenerateHtmlReport(results);
                }

                await File.WriteAllTextAsync(reportPath, reportContent);
                Logger.Information("Security report generated: {Path}", reportPath);

                return reportPath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to generate security report");
                throw;
            }
        }

        private (string? apiUrl, string? apiKey, HttpClient? client) InitializeZapClient()
        {
            try
            {
                if (!_config.OWASPZAPEnabled)
                {
                    Logger.Debug("OWASP ZAP is disabled in configuration");
                    return (null, null, null);
                }

                var apiUrl = _config.OWASPZAPApiUrl;
                var apiKey = _config.OWASPZAPApiKey;

                if (string.IsNullOrEmpty(apiUrl))
                {
                    Logger.Warning("OWASP ZAP API URL is not configured");
                    return (null, null, null);
                }

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(apiUrl),
                    Timeout = TimeSpan.FromMinutes(5)
                };

                try
                {
                    var versionUrl = string.IsNullOrEmpty(apiKey) 
                        ? "/JSON/core/view/version" 
                        : $"/JSON/core/view/version?apikey={apiKey}";
                    var versionResponse = httpClient.GetStringAsync(versionUrl).GetAwaiter().GetResult();
                    Logger.Information("OWASP ZAP client initialized. API URL: {Url}", apiUrl);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Could not verify OWASP ZAP connection: {Error}. Client created but may not be functional.", ex.Message);
                }

                return (apiUrl, apiKey, httpClient);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize OWASP ZAP client: {Error}", ex.Message);
                return (null, null, null);
            }
        }

        private async Task<string> RunSpiderScanAsync(string url)
        {
            if (_httpClient == null || string.IsNullOrEmpty(_zapApiUrl)) 
                throw new InvalidOperationException("ZAP client not initialized");

            var apiKeyParam = string.IsNullOrEmpty(_zapApiKey) ? "" : $"?apikey={_zapApiKey}";
            var response = await _httpClient.GetStringAsync($"{_zapApiUrl}/JSON/spider/action/scan/{apiKeyParam}&url={Uri.EscapeDataString(url)}");
            
            var jsonDoc = JsonDocument.Parse(response);
            return jsonDoc.RootElement.TryGetProperty("scan", out var scanId) 
                ? scanId.GetString() ?? string.Empty 
                : string.Empty;
        }

        private async Task<string> RunActiveScanAsync(string url)
        {
            if (_httpClient == null || string.IsNullOrEmpty(_zapApiUrl)) 
                throw new InvalidOperationException("ZAP client not initialized");

            var apiKeyParam = string.IsNullOrEmpty(_zapApiKey) ? "" : $"?apikey={_zapApiKey}";
            var response = await _httpClient.GetStringAsync($"{_zapApiUrl}/JSON/ascan/action/scan/{apiKeyParam}&url={Uri.EscapeDataString(url)}");
            
            var jsonDoc = JsonDocument.Parse(response);
            return jsonDoc.RootElement.TryGetProperty("scan", out var scanId) 
                ? scanId.GetString() ?? string.Empty 
                : string.Empty;
        }

        private async Task WaitForScanCompletionAsync(string scanId, string scanType)
        {
            if (_httpClient == null || string.IsNullOrEmpty(_zapApiUrl)) return;

            int progress = 0;
            while (progress < 100)
            {
                var apiKeyParam = string.IsNullOrEmpty(_zapApiKey) ? "" : $"?apikey={_zapApiKey}";
                var statusUrl = scanType == "spider"
                    ? $"{_zapApiUrl}/JSON/spider/view/status/{apiKeyParam}&scanId={scanId}"
                    : $"{_zapApiUrl}/JSON/ascan/view/status/{apiKeyParam}&scanId={scanId}";

                var response = await _httpClient.GetStringAsync(statusUrl);
                var jsonDoc = JsonDocument.Parse(response);
                
                if (jsonDoc.RootElement.TryGetProperty("status", out var status))
                {
                    progress = status.GetInt32();
                }

                if (progress < 100)
                {
                    await Task.Delay(2000);
                }
            }
        }

        private VulnerabilitySummary CategorizeVulnerabilities(List<SecurityAlert> alerts)
        {
            return new VulnerabilitySummary
            {
                HighRisk = alerts.Count(a => a.Risk == "High"),
                MediumRisk = alerts.Count(a => a.Risk == "Medium"),
                LowRisk = alerts.Count(a => a.Risk == "Low"),
                Informational = alerts.Count(a => a.Risk == "Informational"),
                Total = alerts.Count
            };
        }

        private string GenerateHtmlReport(SecurityScanResults results)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Security Scan Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }}
        .summary {{ background-color: #ecf0f1; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .high-risk {{ color: #e74c3c; font-weight: bold; }}
        .medium-risk {{ color: #f39c12; font-weight: bold; }}
        .low-risk {{ color: #3498db; font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background-color: #34495e; color: white; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Security Scan Report</h1>
        <p>URL: {results.Url}</p>
        <p>Scan Duration: {results.Duration.TotalSeconds:F2} seconds</p>
    </div>
    <div class='summary'>
        <h2>Summary</h2>
        <p><strong>Total Alerts:</strong> {results.Alerts.Count}</p>
        <p><strong>High Risk:</strong> <span class='high-risk'>{results.Vulnerabilities.HighRisk}</span></p>
        <p><strong>Medium Risk:</strong> <span class='medium-risk'>{results.Vulnerabilities.MediumRisk}</span></p>
        <p><strong>Low Risk:</strong> <span class='low-risk'>{results.Vulnerabilities.LowRisk}</span></p>
    </div>
</body>
</html>";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Security scan results.
    /// </summary>
    public class SecurityScanResults
    {
        public string Url { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool SpiderScanCompleted { get; set; }
        public bool ActiveScanCompleted { get; set; }
        public List<SecurityAlert> Alerts { get; set; } = new();
        public VulnerabilitySummary Vulnerabilities { get; set; } = new();
    }

    /// <summary>
    /// Security alert from ZAP.
    /// </summary>
    public class SecurityAlert
    {
        public string Name { get; set; } = string.Empty;
        public string Risk { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Vulnerability summary.
    /// </summary>
    public class VulnerabilitySummary
    {
        public int HighRisk { get; set; }
        public int MediumRisk { get; set; }
        public int LowRisk { get; set; }
        public int Informational { get; set; }
        public int Total { get; set; }
    }
}

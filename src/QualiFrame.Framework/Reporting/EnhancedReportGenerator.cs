using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Orchestration;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Web.Accessibility;
using QualiFrame.Framework.Security;

namespace QualiFrame.Framework.Reporting
{
    /// <summary>
    /// Enhanced report generator with additional features including accessibility and security reports.
    /// </summary>
    public class EnhancedReportGenerator : ReportGenerator
    {
        /// <summary>
        /// Generates a comprehensive HTML report including accessibility and security results.
        /// </summary>
        public string GenerateComprehensiveReport(
            OrchestrationResult orchestrationResult,
            AnalyticsReport? analyticsReport = null,
            AccessibilityResults? accessibilityResults = null,
            SecurityScanResults? securityResults = null)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Comprehensive Test Execution Report</title>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<style>");
            html.AppendLine(GetEnhancedReportStyles());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Comprehensive Test Execution Report</h1>");
            html.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='executive-summary'>");
            html.AppendLine("<h2>Executive Summary</h2>");
            html.AppendLine("<div class='summary-grid'>");
            html.AppendLine($"<div class='summary-item'><span class='label'>Total Tests:</span><span class='value'>{orchestrationResult.TotalTests}</span></div>");
            html.AppendLine($"<div class='summary-item success'><span class='label'>Passed:</span><span class='value'>{orchestrationResult.Passed}</span></div>");
            html.AppendLine($"<div class='summary-item failure'><span class='label'>Failed:</span><span class='value'>{orchestrationResult.Failed}</span></div>");
            html.AppendLine($"<div class='summary-item skipped'><span class='label'>Skipped:</span><span class='value'>{orchestrationResult.Skipped}</span></div>");
            html.AppendLine($"<div class='summary-item'><span class='label'>Success Rate:</span><span class='value'>{CalculateSuccessRate(orchestrationResult):F2}%</span></div>");
            html.AppendLine($"<div class='summary-item'><span class='label'>Total Duration:</span><span class='value'>{orchestrationResult.TotalDuration.TotalSeconds:F2}s</span></div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            if (analyticsReport != null)
            {
                html.AppendLine("<div class='analytics'>");
                html.AppendLine("<h2>Analytics</h2>");
                html.AppendLine($"<p><strong>Success Rate:</strong> {analyticsReport.Statistics.SuccessRate:F2}%</p>");
                html.AppendLine($"<p><strong>Average Duration:</strong> {analyticsReport.Statistics.AverageDuration.TotalMilliseconds:F0}ms</p>");
                html.AppendLine($"<p><strong>Trend:</strong> {analyticsReport.Trend.TrendDescription}</p>");
                html.AppendLine("</div>");
            }

            if (accessibilityResults != null)
            {
                html.AppendLine("<div class='accessibility'>");
                html.AppendLine("<h2>Accessibility Test Results</h2>");
                html.AppendLine($"<p><strong>URL:</strong> {accessibilityResults.Url}</p>");
                html.AppendLine($"<p><strong>Violations:</strong> <span class='violation-count'>{accessibilityResults.Violations.Count}</span></p>");
                html.AppendLine($"<p><strong>Passes:</strong> <span class='pass-count'>{accessibilityResults.Passes.Count}</span></p>");
                
                if (accessibilityResults.Violations.Count > 0)
                {
                    html.AppendLine("<h3>Violations</h3>");
                    html.AppendLine("<ul>");
                    foreach (var violation in accessibilityResults.Violations.Take(10))
                    {
                        html.AppendLine($"<li><strong>{violation.Id}</strong>: {violation.Description} (Impact: {violation.Impact})</li>");
                    }
                    html.AppendLine("</ul>");
                }
                html.AppendLine("</div>");
            }

            if (securityResults != null)
            {
                html.AppendLine("<div class='security'>");
                html.AppendLine("<h2>Security Scan Results</h2>");
                html.AppendLine($"<p><strong>URL:</strong> {securityResults.Url}</p>");
                html.AppendLine($"<p><strong>Duration:</strong> {securityResults.Duration.TotalSeconds:F2}s</p>");
                html.AppendLine($"<p><strong>Total Alerts:</strong> {securityResults.Alerts.Count}</p>");
                html.AppendLine($"<p><strong>High Risk:</strong> <span class='high-risk'>{securityResults.Vulnerabilities.HighRisk}</span></p>");
                html.AppendLine($"<p><strong>Medium Risk:</strong> <span class='medium-risk'>{securityResults.Vulnerabilities.MediumRisk}</span></p>");
                html.AppendLine($"<p><strong>Low Risk:</strong> <span class='low-risk'>{securityResults.Vulnerabilities.LowRisk}</span></p>");
                html.AppendLine("</div>");
            }

            html.AppendLine("<div class='results'>");
            html.AppendLine("<h2>Test Results</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Test Name</th>");
            html.AppendLine("<th>Status</th>");
            html.AppendLine("<th>Duration</th>");
            html.AppendLine("<th>Error Message</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var result in orchestrationResult.ExecutionResults.OrderBy(r => r.TestName))
            {
                var statusClass = result.Status.ToLower();
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{result.TestName}</td>");
                html.AppendLine($"<td><span class='status {statusClass}'>{result.Status}</span></td>");
                html.AppendLine($"<td>{result.Duration.TotalMilliseconds:F0}ms</td>");
                html.AppendLine($"<td>{result.ErrorMessage ?? "-"}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");

            if (orchestrationResult.Failed > 0)
            {
                html.AppendLine("<div class='failed-tests'>");
                html.AppendLine("<h2>Failed Tests Details</h2>");
                html.AppendLine("<ul>");
                foreach (var failed in orchestrationResult.ExecutionResults.Where(r => r.Status == "Failed"))
                {
                    html.AppendLine($"<li><strong>{failed.TestName}</strong>: {failed.ErrorMessage}</li>");
                }
                html.AppendLine("</ul>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            var htmlContent = html.ToString();
            SaveReport(htmlContent, "comprehensive-test-report.html");
            
            return htmlContent;
        }

        /// <summary>
        /// Generates a JSON report with all test data.
        /// </summary>
        public string GenerateComprehensiveJsonReport(
            OrchestrationResult orchestrationResult,
            AnalyticsReport? analyticsReport = null,
            AccessibilityResults? accessibilityResults = null,
            SecurityScanResults? securityResults = null)
        {
            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                Summary = new
                {
                    TotalTests = orchestrationResult.TotalTests,
                    Passed = orchestrationResult.Passed,
                    Failed = orchestrationResult.Failed,
                    Skipped = orchestrationResult.Skipped,
                    SuccessRate = CalculateSuccessRate(orchestrationResult),
                    TotalDuration = orchestrationResult.TotalDuration.TotalMilliseconds
                },
                Analytics = analyticsReport != null ? new
                {
                    SuccessRate = analyticsReport.Statistics.SuccessRate,
                    AverageDuration = analyticsReport.Statistics.AverageDuration.TotalMilliseconds,
                    Trend = analyticsReport.Trend.TrendDescription
                } : null,
                Accessibility = accessibilityResults != null ? new
                {
                    Url = accessibilityResults.Url,
                    Violations = accessibilityResults.Violations.Count,
                    Passes = accessibilityResults.Passes.Count,
                    Timestamp = accessibilityResults.Timestamp
                } : null,
                Security = securityResults != null ? new
                {
                    Url = securityResults.Url,
                    TotalAlerts = securityResults.Alerts.Count,
                    HighRisk = securityResults.Vulnerabilities.HighRisk,
                    MediumRisk = securityResults.Vulnerabilities.MediumRisk,
                    LowRisk = securityResults.Vulnerabilities.LowRisk,
                    Duration = securityResults.Duration.TotalMilliseconds
                } : null,
                TestResults = orchestrationResult.ExecutionResults.Select(r => new
                {
                    TestName = r.TestName,
                    Status = r.Status,
                    Duration = r.Duration.TotalMilliseconds,
                    ErrorMessage = r.ErrorMessage
                })
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            SaveReport(json, "comprehensive-test-report.json");
            return json;
        }

        /// <summary>
        /// Generates a dashboard-style report with charts and metrics.
        /// </summary>
        public string GenerateDashboardReport(OrchestrationResult orchestrationResult, AnalyticsReport? analyticsReport = null)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Test Execution Dashboard</title>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js'></script>");
            html.AppendLine("<style>");
            html.AppendLine(GetDashboardStyles());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class='dashboard'>");
            html.AppendLine("<h1>Test Execution Dashboard</h1>");
            
            html.AppendLine("<div class='metrics-grid'>");
            html.AppendLine($"<div class='metric-card'><h3>Total Tests</h3><p class='metric-value'>{orchestrationResult.TotalTests}</p></div>");
            html.AppendLine($"<div class='metric-card success'><h3>Passed</h3><p class='metric-value'>{orchestrationResult.Passed}</p></div>");
            html.AppendLine($"<div class='metric-card failure'><h3>Failed</h3><p class='metric-value'>{orchestrationResult.Failed}</p></div>");
            html.AppendLine($"<div class='metric-card'><h3>Success Rate</h3><p class='metric-value'>{CalculateSuccessRate(orchestrationResult):F1}%</p></div>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='chart-container'>");
            html.AppendLine("<canvas id='testResultsChart'></canvas>");
            html.AppendLine("</div>");

            html.AppendLine("<script>");
            html.AppendLine($@"
                var ctx = document.getElementById('testResultsChart').getContext('2d');
                var chart = new Chart(ctx, {{
                    type: 'doughnut',
                    data: {{
                        labels: ['Passed', 'Failed', 'Skipped'],
                        datasets: [{{
                            data: [{orchestrationResult.Passed}, {orchestrationResult.Failed}, {orchestrationResult.Skipped}],
                            backgroundColor: ['#28a745', '#dc3545', '#ffc107']
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        maintainAspectRatio: true
                    }}
                }});
            ");
            html.AppendLine("</script>");

            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            var htmlContent = html.ToString();
            SaveReport(htmlContent, "dashboard-report.html");
            
            return htmlContent;
        }

        private double CalculateSuccessRate(OrchestrationResult result)
        {
            if (result.TotalTests == 0) return 0;
            return (double)result.Passed / result.TotalTests * 100;
        }

        private string GetEnhancedReportStyles()
        {
            return GetReportStyles() + @"
                .executive-summary {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 30px;
                    border-radius: 10px;
                    margin-bottom: 30px;
                    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                }
                .accessibility, .security {
                    background-color: white;
                    padding: 20px;
                    border-radius: 5px;
                    margin-bottom: 20px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    border-left: 4px solid #3498db;
                }
                .violation-count {
                    color: #e74c3c;
                    font-weight: bold;
                }
                .pass-count {
                    color: #27ae60;
                    font-weight: bold;
                }
                .high-risk {
                    color: #e74c3c;
                    font-weight: bold;
                }
                .medium-risk {
                    color: #f39c12;
                    font-weight: bold;
                }
                .low-risk {
                    color: #3498db;
                    font-weight: bold;
                }
            ";
        }

        private string GetDashboardStyles()
        {
            return @"
                body {
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    margin: 0;
                    padding: 20px;
                    background-color: #f5f7fa;
                }
                .dashboard {
                    max-width: 1200px;
                    margin: 0 auto;
                }
                .dashboard h1 {
                    color: #2c3e50;
                    margin-bottom: 30px;
                }
                .metrics-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                    gap: 20px;
                    margin-bottom: 30px;
                }
                .metric-card {
                    background: white;
                    padding: 25px;
                    border-radius: 10px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    text-align: center;
                    border-top: 4px solid #3498db;
                }
                .metric-card.success {
                    border-top-color: #27ae60;
                }
                .metric-card.failure {
                    border-top-color: #e74c3c;
                }
                .metric-card h3 {
                    margin: 0 0 10px 0;
                    color: #7f8c8d;
                    font-size: 14px;
                    text-transform: uppercase;
                }
                .metric-value {
                    font-size: 36px;
                    font-weight: bold;
                    color: #2c3e50;
                    margin: 0;
                }
                .chart-container {
                    background: white;
                    padding: 30px;
                    border-radius: 10px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    height: 400px;
                }
            ";
        }
    }
}

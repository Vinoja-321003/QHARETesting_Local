using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QualiFrame.Framework.Analytics;
using QualiFrame.Framework.Orchestration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Reporting
{
    /// <summary>
    /// Generates comprehensive test reports.
    /// </summary>
    public class ReportGenerator
    {
        private readonly string _reportsPath;

        public ReportGenerator()
        {
            _reportsPath = Path.Combine(Directory.GetCurrentDirectory(), "Results", "Reports");
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Generates HTML report from orchestration results.
        /// </summary>
        public string GenerateHtmlReport(OrchestrationResult orchestrationResult, AnalyticsReport? analyticsReport = null)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Test Execution Report</title>");
            html.AppendLine("<style>");
            html.AppendLine(GetReportStyles());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Test Execution Report</h1>");
            html.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary'>");
            html.AppendLine("<h2>Summary</h2>");
            html.AppendLine("<div class='summary-grid'>");
            html.AppendLine($"<div class='summary-item'><span class='label'>Total Tests:</span><span class='value'>{orchestrationResult.TotalTests}</span></div>");
            html.AppendLine($"<div class='summary-item success'><span class='label'>Passed:</span><span class='value'>{orchestrationResult.Passed}</span></div>");
            html.AppendLine($"<div class='summary-item failure'><span class='label'>Failed:</span><span class='value'>{orchestrationResult.Failed}</span></div>");
            html.AppendLine($"<div class='summary-item skipped'><span class='label'>Skipped:</span><span class='value'>{orchestrationResult.Skipped}</span></div>");
            html.AppendLine($"<div class='summary-item'><span class='label'>Total Duration:</span><span class='value'>{orchestrationResult.TotalDuration.TotalSeconds:F2}s</span></div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            if (analyticsReport != null)
            {
                html.AppendLine("<div class='analytics'>");
                html.AppendLine("<h2>Analytics</h2>");
                html.AppendLine($"<p>Success Rate: {analyticsReport.Statistics.SuccessRate:F2}%</p>");
                html.AppendLine($"<p>Average Duration: {analyticsReport.Statistics.AverageDuration.TotalMilliseconds:F0}ms</p>");
                html.AppendLine($"<p>Trend: {analyticsReport.Trend.TrendDescription}</p>");
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
                html.AppendLine("<h2>Failed Tests</h2>");
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
            SaveReport(htmlContent, "test-report.html");
            
            return htmlContent;
        }

        /// <summary>
        /// Generates JSON report.
        /// </summary>
        public string GenerateJsonReport(OrchestrationResult orchestrationResult, AnalyticsReport? analyticsReport = null)
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
                    TotalDuration = orchestrationResult.TotalDuration.TotalMilliseconds
                },
                Analytics = analyticsReport != null ? new
                {
                    SuccessRate = analyticsReport.Statistics.SuccessRate,
                    AverageDuration = analyticsReport.Statistics.AverageDuration.TotalMilliseconds,
                    Trend = analyticsReport.Trend.TrendDescription
                } : null,
                TestResults = orchestrationResult.ExecutionResults.Select(r => new
                {
                    TestName = r.TestName,
                    Status = r.Status,
                    Duration = r.Duration.TotalMilliseconds,
                    ErrorMessage = r.ErrorMessage
                })
            };

            var json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            SaveReport(json, "test-report.json");
            return json;
        }

        protected void SaveReport(string content, string fileName)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filePath = Path.Combine(_reportsPath, $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}");
                File.WriteAllText(filePath, content);
                Logger.Information("Report saved: {Path}", filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save report: {Error}", ex.Message);
            }
        }

        protected string GetReportStyles()
        {
            return @"
                body {
                    font-family: Arial, sans-serif;
                    margin: 20px;
                    background-color: #f5f5f5;
                }
                .header {
                    background-color: #2c3e50;
                    color: white;
                    padding: 20px;
                    border-radius: 5px;
                    margin-bottom: 20px;
                }
                .summary {
                    background-color: white;
                    padding: 20px;
                    border-radius: 5px;
                    margin-bottom: 20px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                }
                .summary-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                    gap: 15px;
                    margin-top: 15px;
                }
                .summary-item {
                    padding: 15px;
                    border-radius: 5px;
                    background-color: #ecf0f1;
                }
                .summary-item.success {
                    background-color: #d4edda;
                }
                .summary-item.failure {
                    background-color: #f8d7da;
                }
                .summary-item.skipped {
                    background-color: #fff3cd;
                }
                .summary-item .label {
                    display: block;
                    font-weight: bold;
                    margin-bottom: 5px;
                }
                .summary-item .value {
                    font-size: 24px;
                    font-weight: bold;
                }
                .analytics {
                    background-color: white;
                    padding: 20px;
                    border-radius: 5px;
                    margin-bottom: 20px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                }
                .results {
                    background-color: white;
                    padding: 20px;
                    border-radius: 5px;
                    margin-bottom: 20px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                }
                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 15px;
                }
                th, td {
                    padding: 12px;
                    text-align: left;
                    border-bottom: 1px solid #ddd;
                }
                th {
                    background-color: #34495e;
                    color: white;
                }
                tr:hover {
                    background-color: #f5f5f5;
                }
                .status {
                    padding: 5px 10px;
                    border-radius: 3px;
                    font-weight: bold;
                }
                .status.passed {
                    background-color: #28a745;
                    color: white;
                }
                .status.failed {
                    background-color: #dc3545;
                    color: white;
                }
                .status.skipped {
                    background-color: #ffc107;
                    color: black;
                }
                .failed-tests {
                    background-color: white;
                    padding: 20px;
                    border-radius: 5px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                }
                .failed-tests ul {
                    list-style-type: none;
                    padding: 0;
                }
                .failed-tests li {
                    padding: 10px;
                    margin: 5px 0;
                    background-color: #f8d7da;
                    border-left: 4px solid #dc3545;
                }
            ";
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                Directory.CreateDirectory(_reportsPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create reports directory: {Error}", ex.Message);
            }
        }
    }
}

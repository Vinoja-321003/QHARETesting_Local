using System;
using System.Collections.Generic;
using System.Linq;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Web.Accessibility
{
    /// <summary>
    /// WCAG compliance checker and reporter.
    /// </summary>
    public class WCAGComplianceChecker
    {
        /// <summary>
        /// Checks WCAG compliance level.
        /// </summary>
        public WCAGComplianceReport CheckCompliance(AccessibilityResults results, string level = "AA")
        {
            var report = new WCAGComplianceReport
            {
                Level = level,
                TotalViolations = results.Violations.Count,
                CriticalViolations = results.Violations.Count(v => v.Impact == "critical"),
                SeriousViolations = results.Violations.Count(v => v.Impact == "serious"),
                ModerateViolations = results.Violations.Count(v => v.Impact == "moderate"),
                MinorViolations = results.Violations.Count(v => v.Impact == "minor"),
                TotalPasses = results.Passes.Count,
                CompliancePercentage = CalculateCompliancePercentage(results),
                IsCompliant = results.Violations.Count == 0,
                Url = results.Url,
                Timestamp = results.Timestamp
            };

            report.ComplianceStatus = report.IsCompliant 
                ? "Compliant" 
                : report.CriticalViolations > 0 || report.SeriousViolations > 0 
                    ? "Non-Compliant" 
                    : "Partially Compliant";

            Logger.Information("WCAG {Level} Compliance: {Status}, Violations: {Violations}, Passes: {Passes}",
                level, report.ComplianceStatus, report.TotalViolations, report.TotalPasses);

            return report;
        }

        /// <summary>
        /// Generates a compliance report in HTML format.
        /// </summary>
        public string GenerateHtmlReport(WCAGComplianceReport report)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>WCAG {report.Level} Compliance Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }}
        .summary {{ background-color: #ecf0f1; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .compliant {{ color: #27ae60; font-weight: bold; }}
        .non-compliant {{ color: #e74c3c; font-weight: bold; }}
        .partially {{ color: #f39c12; font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background-color: #34495e; color: white; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>WCAG {report.Level} Compliance Report</h1>
        <p>Generated: {report.Timestamp:yyyy-MM-dd HH:mm:ss}</p>
        <p>URL: {report.Url}</p>
    </div>
    <div class='summary'>
        <h2>Summary</h2>
        <p><strong>Status:</strong> <span class='{report.ComplianceStatus.ToLower().Replace(" ", "-")}'>{report.ComplianceStatus}</span></p>
        <p><strong>Compliance:</strong> {report.CompliancePercentage:F2}%</p>
        <p><strong>Total Violations:</strong> {report.TotalViolations}</p>
        <p><strong>Critical:</strong> {report.CriticalViolations}, <strong>Serious:</strong> {report.SeriousViolations}</p>
        <p><strong>Total Passes:</strong> {report.TotalPasses}</p>
    </div>
</body>
</html>";

            return html;
        }

        private double CalculateCompliancePercentage(AccessibilityResults results)
        {
            var total = results.Violations.Count + results.Passes.Count;
            if (total == 0) return 100.0;
            return (double)results.Passes.Count / total * 100.0;
        }
    }

    /// <summary>
    /// WCAG compliance report.
    /// </summary>
    public class WCAGComplianceReport
    {
        public string Level { get; set; } = string.Empty;
        public int TotalViolations { get; set; }
        public int CriticalViolations { get; set; }
        public int SeriousViolations { get; set; }
        public int ModerateViolations { get; set; }
        public int MinorViolations { get; set; }
        public int TotalPasses { get; set; }
        public double CompliancePercentage { get; set; }
        public bool IsCompliant { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

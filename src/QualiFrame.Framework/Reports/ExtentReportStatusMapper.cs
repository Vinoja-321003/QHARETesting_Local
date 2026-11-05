using AventStack.ExtentReports;
using System;

namespace QualiFrame.Framework.Reports
{
    /// <summary>
    /// Maps custom status strings to ExtentReports Status enum.
    /// Implements Java framework's status mapping pattern (DONE→INFO, DEBUG→WARNING).
    /// </summary>
    public static class ExtentReportStatusMapper
    {
        /// <summary>
        /// Maps a status string to ExtentReports Status enum.
        /// Handles Java framework-style status mappings:
        /// - DONE, SCREENSHOTS → INFO
        /// - DEBUG → WARNING
        /// - PASS, PASSED → Pass
        /// - FAIL, FAILED → Fail
        /// - SKIP, SKIPPED → Skip
        /// </summary>
        public static Status MapStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Status.Info;
            }

            var normalizedStatus = status.Trim().ToUpperInvariant();

            // Java framework mappings
            if (normalizedStatus.Contains("DONE") || normalizedStatus.Contains("SCREENSHOTS"))
            {
                return Status.Info;
            }

            if (normalizedStatus.Contains("DEBUG"))
            {
                return Status.Warning;
            }

            // Standard status mappings
            return normalizedStatus switch
            {
                "PASS" or "PASSED" => Status.Pass,
                "FAIL" or "FAILED" => Status.Fail,
                "SKIP" or "SKIPPED" => Status.Skip,
                "INFO" or "INFORMATION" => Status.Info,
                "WARNING" or "WARN" => Status.Warning,
                "ERROR" => Status.Error,
                "FATAL" => Status.Error, // Fatal maps to Error (Fatal not available in ExtentReports)
                _ => Status.Info // Default to Info for unknown statuses
            };
        }

        /// <summary>
        /// Maps a boolean result to ExtentReports Status.
        /// </summary>
        public static Status MapStatus(bool passed)
        {
            return passed ? Status.Pass : Status.Fail;
        }

        /// <summary>
        /// Maps an exception to ExtentReports Status (always Fail).
        /// </summary>
        public static Status MapStatus(Exception exception)
        {
            return Status.Fail;
        }
    }
}

using System;
using System.IO;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Reporting
{
    /// <summary>
    /// Allure test reporter integration wrapper.
    /// Note: Allure.NUnit integrates automatically with NUnit tests via attributes.
    /// This wrapper provides programmatic access for non-NUnit scenarios.
    /// </summary>
    public class AllureReporter
    {
        private readonly TestConfiguration _config;
        private readonly string _allureResultsPath;

        public AllureReporter()
        {
            _config = TestConfiguration.Instance;
            _allureResultsPath = Path.Combine(Directory.GetCurrentDirectory(), "Results", "Allure");
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Adds an attachment to the current test.
        /// For NUnit tests, use Allure.Attachments API directly.
        /// </summary>
        public void AddAttachment(string name, byte[] content, string contentType = "application/octet-stream")
        {
            try
            {
                var filePath = Path.Combine(_allureResultsPath, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}");
                File.WriteAllBytes(filePath, content);
                Logger.Debug("Attachment saved: {Path}", filePath);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to add attachment: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Adds a screenshot attachment.
        /// </summary>
        public void AddScreenshot(string name, byte[] screenshot)
        {
            AddAttachment($"{name}.png", screenshot, "image/png");
        }

        /// <summary>
        /// Logs a step for the current test.
        /// For NUnit tests, use Allure.Step attributes.
        /// </summary>
        public void LogStep(string stepName, string? details = null)
        {
            Logger.Information("Allure Step: {StepName} - {Details}", stepName, details ?? "");
        }

        /// <summary>
        /// Logs test metadata.
        /// For NUnit tests, use Allure attributes: [AllureEpic], [AllureFeature], [AllureStory], [AllureSeverity]
        /// </summary>
        public void LogMetadata(string epic, string feature, string story, SeverityLevel severity)
        {
            Logger.Debug("Allure Metadata - Epic: {Epic}, Feature: {Feature}, Story: {Story}, Severity: {Severity}",
                epic, feature, story, severity);
        }

        /// <summary>
        /// Gets the Allure results directory path.
        /// </summary>
        public string GetResultsPath()
        {
            return _allureResultsPath;
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                Directory.CreateDirectory(_allureResultsPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create Allure results directory: {Error}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Allure severity levels.
    /// </summary>
    public enum SeverityLevel
    {
        Blocker,
        Critical,
        Normal,
        Minor,
        Trivial
    }
}

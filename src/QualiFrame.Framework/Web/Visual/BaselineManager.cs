using System;
using System.IO;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Web.Visual
{
    /// <summary>
    /// Manages baseline screenshots for visual regression testing.
    /// </summary>
    public class BaselineManager
    {
        private readonly string _baselinePath;
        private readonly TestConfiguration _config;

        public BaselineManager(string? baselinePath = null)
        {
            _config = TestConfiguration.Instance;
            _baselinePath = baselinePath ?? Path.Combine(
                Directory.GetCurrentDirectory(),
                "Results",
                "Baselines",
                _config.Browser);
            
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Saves a screenshot as baseline.
        /// </summary>
        public void SaveBaseline(string testName, byte[] screenshot)
        {
            var fileName = SanitizeFileName($"{testName}.png");
            var filePath = Path.Combine(_baselinePath, fileName);

            try
            {
                File.WriteAllBytes(filePath, screenshot);
                Logger.Information("Baseline saved: {Path}", filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save baseline: {Path}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Gets baseline screenshot if it exists.
        /// </summary>
        public byte[]? GetBaseline(string testName)
        {
            var fileName = SanitizeFileName($"{testName}.png");
            var filePath = Path.Combine(_baselinePath, fileName);

            if (!File.Exists(filePath))
            {
                Logger.Warning("Baseline not found: {Path}", filePath);
                return null;
            }

            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to read baseline: {Path}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Checks if baseline exists.
        /// </summary>
        public bool BaselineExists(string testName)
        {
            var fileName = SanitizeFileName($"{testName}.png");
            var filePath = Path.Combine(_baselinePath, fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Updates baseline with new screenshot.
        /// </summary>
        public void UpdateBaseline(string testName, byte[] screenshot)
        {
            SaveBaseline(testName, screenshot);
            Logger.Information("Baseline updated: {TestName}", testName);
        }

        /// <summary>
        /// Deletes baseline.
        /// </summary>
        public void DeleteBaseline(string testName)
        {
            var fileName = SanitizeFileName($"{testName}.png");
            var filePath = Path.Combine(_baselinePath, fileName);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Logger.Information("Baseline deleted: {Path}", filePath);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to delete baseline: {Path}", filePath);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the baseline directory path.
        /// </summary>
        public string GetBaselinePath()
        {
            return _baselinePath;
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                Directory.CreateDirectory(_baselinePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create baseline directory: {Path}", _baselinePath);
                throw;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}

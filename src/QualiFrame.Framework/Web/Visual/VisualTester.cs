using System;
using System.IO;
using OpenQA.Selenium;
using SkiaSharp;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Framework.Web.Visual
{
    /// <summary>
    /// Visual testing utility for screenshot comparison and visual regression detection.
    /// </summary>
    public class VisualTester
    {
        private readonly BaselineManager _baselineManager;
        private readonly TestConfiguration _config;
        private readonly double _threshold;

        public VisualTester(double threshold = 0.01)
        {
            _config = TestConfiguration.Instance;
            _baselineManager = new BaselineManager();
            _threshold = threshold;
        }

        /// <summary>
        /// Captures screenshot and compares with baseline.
        /// </summary>
        public VisualTestResult CompareWithBaseline(IWebDriver driver, string testName)
        {
            try
            {
                var screenshot = CaptureScreenshot(driver);
                var actualPath = SaveActualScreenshot(testName, screenshot);

                var baseline = _baselineManager.GetBaseline(testName);

                if (baseline == null)
                {
                    _baselineManager.SaveBaseline(testName, screenshot);
                    Logger.Information("No baseline found for {TestName}. Saved as new baseline.", testName);

                    return new VisualTestResult
                    {
                        TestName = testName,
                        IsMatch = true,
                        BaselineCreated = true,
                        Message = "Baseline created (no existing baseline found)"
                    };
                }

                var comparison = ScreenshotComparator.Compare(baseline, screenshot, _threshold);
                var diffImage = ScreenshotComparator.GenerateDiffImage(baseline, screenshot);
                var diffPath = SaveDiffImage(testName, diffImage);

                var result = new VisualTestResult
                {
                    TestName = testName,
                    IsMatch = comparison.IsMatch,
                    DifferencePercentage = comparison.DifferencePercentage,
                    Threshold = comparison.Threshold,
                    BaselinePath = _baselineManager.GetBaselinePath(),
                    ActualPath = actualPath,
                    DiffPath = diffPath,
                    BaselineCreated = false
                };

                if (!comparison.IsMatch)
                {
                    Logger.Warning(
                        "Visual regression detected: {TestName}, Difference: {Difference:P2}",
                        testName, comparison.DifferencePercentage);
                    result.Message = $"Visual regression detected. Difference: {comparison.DifferencePercentage:P2}";
                }
                else
                {
                    Logger.Information("Visual test passed: {TestName}", testName);
                    result.Message = "Visual test passed";
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Visual test failed: {TestName}", testName);
                throw;
            }
        }

        /// <summary>
        /// Captures screenshot of the current page.
        /// </summary>
        public byte[] CaptureScreenshot(IWebDriver driver)
        {
            try
            {
                if (!DriverHealthChecker.IsHealthy(driver))
                {
                    throw new InvalidOperationException("Driver is not healthy");
                }

                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                return screenshot.AsByteArray;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to capture screenshot");
                throw;
            }
        }

        /// <summary>
        /// Captures screenshot of a specific element.
        /// </summary>
        public byte[] CaptureElementScreenshot(IWebDriver driver, IWebElement element)
        {
            try
            {
                var fullScreenshot = CaptureScreenshot(driver);
                var elementLocation = element.Location;
                var elementSize = element.Size;
                using var bitmap = SKBitmap.Decode(fullScreenshot);
                if (bitmap == null)
                {
                    throw new InvalidOperationException("Unable to decode screenshot image.");
                }

                var x = elementLocation.X;
                var y = elementLocation.Y;
                var width = Math.Min(elementSize.Width, bitmap.Width - x);
                var height = Math.Min(elementSize.Height, bitmap.Height - y);

                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x + width > bitmap.Width) width = bitmap.Width - x;
                if (y + height > bitmap.Height) height = bitmap.Height - y;

                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Calculated crop rectangle is invalid.");
                }

                using var cropped = new SKBitmap(width, height);
                using (var canvas = new SKCanvas(cropped))
                {
                    var srcRect = new SKRectI(x, y, x + width, y + height);
                    var destRect = new SKRectI(0, 0, width, height);
                    canvas.DrawBitmap(bitmap, srcRect, destRect);
                }

                using var image = SKImage.FromBitmap(cropped);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to capture element screenshot");
                throw;
            }
        }

        /// <summary>
        /// Updates baseline with current screenshot.
        /// </summary>
        public void UpdateBaseline(IWebDriver driver, string testName)
        {
            var screenshot = CaptureScreenshot(driver);
            _baselineManager.UpdateBaseline(testName, screenshot);
            Logger.Information("Baseline updated for: {TestName}", testName);
        }

        /// <summary>
        /// Compares two screenshots directly.
        /// </summary>
        public ComparisonResult CompareScreenshots(byte[] baseline, byte[] actual)
        {
            return ScreenshotComparator.Compare(baseline, actual, _threshold);
        }

        private string SaveActualScreenshot(string testName, byte[] screenshot)
        {
            var fileName = $"{testName}_actual_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(_config.ScreenshotPath, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllBytes(filePath, screenshot);
            
            return filePath;
        }

        private string SaveDiffImage(string testName, byte[] diffImage)
        {
            var fileName = $"{testName}_diff_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(_config.ScreenshotPath, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllBytes(filePath, diffImage);
            
            return filePath;
        }
    }

    /// <summary>
    /// Result of visual test.
    /// </summary>
    public class VisualTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public bool IsMatch { get; set; }
        public double DifferencePercentage { get; set; }
        public double Threshold { get; set; }
        public string? BaselinePath { get; set; }
        public string? ActualPath { get; set; }
        public string? DiffPath { get; set; }
        public bool BaselineCreated { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

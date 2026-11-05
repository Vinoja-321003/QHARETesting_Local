using System;
using System.IO;
using SkiaSharp;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Web.Visual
{
    /// <summary>
    /// Compares screenshots and detects visual differences.
    /// </summary>
    public class ScreenshotComparator
    {
        /// <summary>
        /// Compares two screenshots and returns the difference percentage.
        /// </summary>
        public static ComparisonResult Compare(byte[] baselineImage, byte[] actualImage, double threshold = 0.01)
        {
            try
            {
                using var baselineBitmap = SKBitmap.Decode(baselineImage);
                using var actualBitmap = SKBitmap.Decode(actualImage);

                if (baselineBitmap == null || actualBitmap == null)
                {
                    throw new InvalidOperationException("Unable to decode one or both images for comparison.");
                }

                SKBitmap baseline;
                SKBitmap actual;

                if (baselineBitmap.Width != actualBitmap.Width || baselineBitmap.Height != actualBitmap.Height)
                {
                    Logger.Warning(
                        "Image dimensions differ: Baseline {BaselineWidth}x{BaselineHeight}, Actual {ActualWidth}x{ActualHeight}",
                        baselineBitmap.Width, baselineBitmap.Height, actualBitmap.Width, actualBitmap.Height);

                    var targetWidth = Math.Max(baselineBitmap.Width, actualBitmap.Width);
                    var targetHeight = Math.Max(baselineBitmap.Height, actualBitmap.Height);

                    baseline = ResizeBitmap(baselineBitmap, targetWidth, targetHeight);
                    actual = ResizeBitmap(actualBitmap, targetWidth, targetHeight);
                }
                else
                {
                    baseline = baselineBitmap;
                    actual = actualBitmap;
                }

                using (baseline)
                using (actual)
                {
                    var difference = CalculateDifference(baseline, actual);
                    var isMatch = difference <= threshold;

                    Logger.Information(
                        "Screenshot comparison: Difference {Difference:P2}, Threshold {Threshold:P2}, Match: {Match}",
                        difference, threshold, isMatch);

                    return new ComparisonResult
                    {
                        IsMatch = isMatch,
                        DifferencePercentage = difference,
                        Threshold = threshold,
                        BaselineWidth = baseline.Width,
                        BaselineHeight = baseline.Height,
                        ActualWidth = actual.Width,
                        ActualHeight = actual.Height
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to compare screenshots: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Compares screenshots from file paths.
        /// </summary>
        public static ComparisonResult CompareFiles(string baselinePath, string actualPath, double threshold = 0.01)
        {
            if (!File.Exists(baselinePath))
            {
                throw new FileNotFoundException($"Baseline image not found: {baselinePath}");
            }

            if (!File.Exists(actualPath))
            {
                throw new FileNotFoundException($"Actual image not found: {actualPath}");
            }

            var baselineImage = File.ReadAllBytes(baselinePath);
            var actualImage = File.ReadAllBytes(actualPath);

            return Compare(baselineImage, actualImage, threshold);
        }

        /// <summary>
        /// Generates a diff image highlighting differences.
        /// </summary>
        public static byte[] GenerateDiffImage(byte[] baselineImage, byte[] actualImage)
        {
            try
            {
                using var baselineBitmap = SKBitmap.Decode(baselineImage);
                using var actualBitmap = SKBitmap.Decode(actualImage);

                if (baselineBitmap == null || actualBitmap == null)
                {
                    throw new InvalidOperationException("Unable to decode one or both images for diff generation.");
                }

                int width = Math.Max(baselineBitmap.Width, actualBitmap.Width);
                int height = Math.Max(baselineBitmap.Height, actualBitmap.Height);

                using var baseline = ResizeBitmap(baselineBitmap, width, height);
                using var actual = ResizeBitmap(actualBitmap, width, height);
                using var diffBitmap = new SKBitmap(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var baselinePixel = baseline.GetPixel(x, y);
                        var actualPixel = actual.GetPixel(x, y);

                        if (!baselinePixel.Equals(actualPixel))
                        {
                            diffBitmap.SetPixel(x, y, new SKColor(255, 0, 0, 255));
                        }
                        else
                        {
                            var gray = (byte)((baselinePixel.Red + baselinePixel.Green + baselinePixel.Blue) / 3);
                            diffBitmap.SetPixel(x, y, new SKColor(gray, gray, gray, 255));
                        }
                    }
                }

                using var image = SKImage.FromBitmap(diffBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to generate diff image: {Error}", ex.Message);
                throw;
            }
        }

        private static double CalculateDifference(SKBitmap baseline, SKBitmap actual)
        {
            long totalDifference = 0;
            long totalPixels = (long)baseline.Width * baseline.Height;

            for (int y = 0; y < baseline.Height; y++)
            {
                for (int x = 0; x < baseline.Width; x++)
                {
                    var baselinePixel = baseline.GetPixel(x, y);
                    var actualPixel = actual.GetPixel(x, y);

                    var rDiff = Math.Abs(baselinePixel.Red - actualPixel.Red);
                    var gDiff = Math.Abs(baselinePixel.Green - actualPixel.Green);
                    var bDiff = Math.Abs(baselinePixel.Blue - actualPixel.Blue);
                    var aDiff = Math.Abs(baselinePixel.Alpha - actualPixel.Alpha);

                    totalDifference += rDiff + gDiff + bDiff + aDiff;
                }
            }

            var maxDifference = totalPixels * 1020.0;
            return totalDifference / maxDifference;
        }

        private static SKBitmap ResizeBitmap(SKBitmap source, int width, int height)
        {
            if (source.Width == width && source.Height == height)
            {
                return source.Copy();
            }

            var resized = new SKBitmap(width, height);
            using var canvas = new SKCanvas(resized);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(
                source,
                new SKRect(0, 0, source.Width, source.Height),
                new SKRect(0, 0, width, height));
            return resized;
        }
    }

    /// <summary>
    /// Result of screenshot comparison.
    /// </summary>
    public class ComparisonResult
    {
        public bool IsMatch { get; set; }
        public double DifferencePercentage { get; set; }
        public double Threshold { get; set; }
        public int BaselineWidth { get; set; }
        public int BaselineHeight { get; set; }
        public int ActualWidth { get; set; }
        public int ActualHeight { get; set; }
    }
}

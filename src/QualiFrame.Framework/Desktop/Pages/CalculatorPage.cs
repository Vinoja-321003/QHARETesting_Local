using System;
using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Drivers;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Pages
{
    /// <summary>
    /// Page object for the Windows Calculator application.
    /// Encapsulates all UI interactions and adds retry-safe helpers for stability.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class CalculatorPage : DesktopPage
    {
        private DesktopElement Num4 => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "num4Button" });

        private DesktopElement Num6 => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "num6Button" });

        private DesktopElement PlusButton => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "plusButton" });

        private DesktopElement EqualsButton => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "equalButton" });

        private DesktopElement Result => new DesktopElement(
            Session,
            new LocatorDefinition { AccessibilityId = "CalculatorResults" });

        /// <summary>
        /// Creates a new instance of <see cref="CalculatorPage"/> using the shared FlaUI session.
        /// </summary>
        public CalculatorPage()
            : base(DesktopSessionFactory.GetSession())
        {
        }

        /// <summary>
        /// Waits until the Windows Calculator application is ready.
        /// Launch is handled by <see cref="DesktopSessionFactory"/> from Desktop:AppId.
        /// </summary>
        public void LaunchCalculator()
        {
            Logger.Information("Waiting for Windows Calculator to become ready.");
            ExtentReportManager.LogInfo("Waiting for Windows Calculator to become ready.");

            WaitForLoaded();

            Logger.Information("Windows Calculator is ready.");
            ExtentReportManager.LogPass("Windows Calculator launched successfully.");
        }

        /// <inheritdoc />
        public override void WaitForLoaded(int timeoutSeconds = 10)
        {
            Logger.Information("Waiting for Calculator main window to load (timeout: {Timeout}s).", timeoutSeconds);

            WaitUntilVisible(
                s => s.SearchRoot.FindFirstDescendant(cf => cf.ByAutomationId("CalculatorResults")),
                timeoutSeconds);
        }

        /// <summary>
        /// Presses a numeric button on the Calculator (supports multi-digit numbers).
        /// </summary>
        public void PressNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                throw new ArgumentException("Number cannot be null or empty.", nameof(number));
            }

            Logger.Information("Pressing Calculator number: {Number}", number);
            ExtentReportManager.LogInfo($"Pressing Calculator number: {number}");

            foreach (var digit in number.Trim())
            {
                if (!char.IsDigit(digit))
                {
                    throw new InvalidOperationException($"Calculator only supports numeric digits for this step. Invalid character: '{digit}'.");
                }

                if (digit == '4')
                {
                    Num4.Click();
                }
                else if (digit == '6')
                {
                    Num6.Click();
                }
                else
                {
                    var locator = new LocatorDefinition { AccessibilityId = $"num{digit}Button" };
                    var element = new DesktopElement(Session, locator);
                    QualiFrame.Framework.Desktop.Actions.DesktopActions.Click(element, $"Press number '{digit}'");
                }
            }

            Logger.Information("Successfully pressed Calculator number: {Number}", number);
            ExtentReportManager.LogPass($"Successfully pressed Calculator number: {number}");
        }

        /// <summary>
        /// Presses the addition operator (+) in the Calculator.
        /// </summary>
        public void PressAddition()
        {
            Logger.Information("Pressing Calculator addition operator (+).");
            ExtentReportManager.LogInfo("Pressing Calculator addition operator (+).");

            PlusButton.Click();

            Logger.Information("Successfully pressed Calculator addition operator (+).");
            ExtentReportManager.LogPass("Successfully pressed Calculator addition operator (+).");
        }

        /// <summary>
        /// Presses the equals (=) button in the Calculator.
        /// </summary>
        public void PressEquals()
        {
            Logger.Information("Pressing Calculator equals (=) button.");
            ExtentReportManager.LogInfo("Pressing Calculator equals (=) button.");

            EqualsButton.Click();

            Logger.Information("Successfully pressed Calculator equals (=) button.");
            ExtentReportManager.LogPass("Successfully pressed Calculator equals (=) button.");
        }

        /// <summary>
        /// Gets the numeric result currently displayed by the Calculator.
        /// </summary>
        public string GetDisplayedResult()
        {
            Logger.Information("Retrieving Calculator displayed result.");
            ExtentReportManager.LogInfo("Retrieving Calculator displayed result.");

            var rawText = Result.GetText() ?? string.Empty;
            var normalized = rawText
                .Replace("Display is", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            Logger.Information("Calculator raw result text: '{Raw}'. Normalized result: '{Result}'.", rawText, normalized);
            ExtentReportManager.LogInfo($"Calculator raw result text: '{rawText}'. Normalized result: '{normalized}'.");

            return normalized;
        }

        /// <summary>
        /// Attempts to close the Calculator application safely.
        /// </summary>
        public void CloseCalculator()
        {
            Logger.Information("Closing Windows Calculator application.");
            ExtentReportManager.LogInfo("Closing Windows Calculator application.");

            try
            {
                Session.CurrentWindow.Close();
                Logger.Information("Calculator window close requested.");
                ExtentReportManager.LogPass("Calculator window close requested.");
            }
            catch (Exception ex)
            {
                Logger.Warning("Error while closing Calculator: {Error}", ex.Message);
                ExtentReportManager.LogWarning($"Error while closing Calculator: {ex.Message}");
            }
        }
    }
}

using FluentAssertions;
using Reqnroll;
using System.Runtime.Versioning;
using QualiFrame.Framework.Desktop.Pages;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.StepDefinitions
{
    /// <summary>
    /// Reqnroll step bindings for Windows Calculator scenarios.
    /// Uses CalculatorPage to encapsulate all UI interactions.
    /// </summary>
    [Binding]
    [SupportedOSPlatform("windows")]
    public class CalculatorSteps
    {
        private readonly CalculatorPage _calculatorPage;

        public CalculatorSteps()
        {
            _calculatorPage = new CalculatorPage();
        }

        [Given(@"the Calculator application is launched")]
        public void GivenTheCalculatorApplicationIsLaunched()
        {
            Logger.Information("Step: Launch Calculator application.");
            ExtentReportManager.LogInfo("Step: Launch Calculator application.");

            _calculatorPage.LaunchCalculator();
        }

        [When(@"I press the number ""(.*)""")]
        public void WhenIPressTheNumber(string number)
        {
            Logger.Information("Step: Press Calculator number {Number}.", number);
            ExtentReportManager.LogInfo($"Step: Press Calculator number {number}.");

            _calculatorPage.PressNumber(number);
        }

        [When(@"I press the addition operator")]
        public void WhenIPressTheAdditionOperator()
        {
            Logger.Information("Step: Press Calculator addition operator.");
            ExtentReportManager.LogInfo("Step: Press Calculator addition operator.");

            _calculatorPage.PressAddition();
        }

        [When(@"I press equals")]
        public void WhenIPressEquals()
        {
            Logger.Information("Step: Press Calculator equals button.");
            ExtentReportManager.LogInfo("Step: Press Calculator equals button.");

            _calculatorPage.PressEquals();
        }

        [Then(@"the result should be ""(.*)""")]
        public void ThenTheResultShouldBe(string expectedResult)
        {
            Logger.Information("Step: Validate Calculator result equals {Expected}.", expectedResult);
            ExtentReportManager.LogInfo($"Step: Validate Calculator result equals {expectedResult}.");

            var actualResult = _calculatorPage.GetDisplayedResult();

            if (!string.Equals(actualResult, expectedResult, System.StringComparison.OrdinalIgnoreCase))
            {
                var message =
                    $"Calculator result validation failed. Expected: {expectedResult} but actual result was: {actualResult}";

                Logger.Error(message);
                ExtentReportManager.LogFail(message);
            }

            actualResult.Should().Be(
                expectedResult,
                $"Calculator should display the expected result. Expected: {expectedResult}, Actual: {actualResult}");

            ExtentReportManager.LogPass($"Calculator result validation passed. Result: {actualResult}");
        }

        [Then(@"the Calculator application is closed")]
        public void ThenTheCalculatorApplicationIsClosed()
        {
            Logger.Information("Step: Close Calculator application.");
            ExtentReportManager.LogInfo("Step: Close Calculator application.");

            _calculatorPage.CloseCalculator();
        }
    }
}


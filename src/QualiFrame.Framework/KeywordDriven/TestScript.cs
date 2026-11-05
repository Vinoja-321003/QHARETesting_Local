using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Test script engine for executing keyword-driven tests.
    /// </summary>
    public class TestScript
    {
        private readonly KeywordRegistry _registry;
        private readonly TestConfiguration _config;
        private IWebDriver? _driver;

        public TestScript()
        {
            _registry = KeywordRegistry.Instance;
            _config = TestConfiguration.Instance;
        }

        /// <summary>
        /// Executes a list of keyword test steps.
        /// </summary>
        public TestScriptResult ExecuteTest(List<KeywordTestStep> steps, string testCaseId)
        {
            var result = new TestScriptResult
            {
                TestCaseId = testCaseId,
                StartTime = DateTime.UtcNow
            };

            try
            {
                Logger.Information("Starting keyword-driven test execution: {TestCaseId}", testCaseId);

                _driver = EnhancedWebDriverFactory.CreateDriver();
                EnhancedWebDriverFactory.SetCurrentDriver(_driver);

                if (!string.IsNullOrEmpty(_config.BaseUrl))
                {
                    _driver.Navigate().GoToUrl(_config.BaseUrl);
                }

                foreach (var step in steps.OrderBy(s => s.StepNumber))
                {
                    Logger.Information("Executing step {StepNumber}: {KeywordName} - {Description}", 
                        step.StepNumber, step.KeywordName, step.Description);

                    var keyword = _registry.GetKeyword(step.KeywordName);
                    if (keyword == null)
                    {
                        var error = $"Keyword '{step.KeywordName}' is not registered";
                        Logger.Error(error);
                        step.Result = KeywordResult.Fail(error);
                        result.FailedSteps.Add(step);
                        result.OverallSuccess = false;
                        continue;
                    }

                    step.Result = keyword.Execute(_driver, step.Parameters);

                    if (step.Result.Success)
                    {
                        result.PassedSteps.Add(step);
                        Logger.Information("Step {StepNumber} passed: {KeywordName}", step.StepNumber, step.KeywordName);
                    }
                    else
                    {
                        if (step.ExpectedToFail)
                        {
                            result.ExpectedFailures.Add(step);
                            Logger.Information("Step {StepNumber} failed as expected: {KeywordName}", step.StepNumber, step.KeywordName);
                        }
                        else
                        {
                            result.FailedSteps.Add(step);
                            result.OverallSuccess = false;
                            Logger.Warning("Step {StepNumber} failed: {KeywordName}, Error: {Error}", 
                                step.StepNumber, step.KeywordName, step.Result.Message);
                        }
                    }
                }

                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.TotalSteps = steps.Count;

                Logger.Information("Keyword-driven test execution completed: {TestCaseId}, " +
                    "Total: {Total}, Passed: {Passed}, Failed: {Failed}, Success: {Success}",
                    testCaseId, result.TotalSteps, result.PassedSteps.Count, 
                    result.FailedSteps.Count, result.OverallSuccess);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing keyword-driven test: {TestCaseId}", testCaseId);
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.OverallSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                try
                {
                    _driver?.Quit();
                    _driver?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warning("Error cleaning up WebDriver: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Executes a keyword-driven test from an Excel file.
        /// </summary>
        public TestScriptResult ExecuteTestFromExcel(string filePath, string sheetName, string testCaseId)
        {
            var parser = new ExcelKeywordParser();
            var steps = parser.ParseTestSteps(filePath, sheetName, testCaseId);

            if (steps.Count == 0)
            {
                Logger.Warning("No test steps found for test case ID: {TestCaseId} in {FilePath}", testCaseId, filePath);
                return new TestScriptResult
                {
                    TestCaseId = testCaseId,
                    OverallSuccess = false,
                    ErrorMessage = $"No test steps found for test case ID: {testCaseId}"
                };
            }

            return ExecuteTest(steps, testCaseId);
        }
    }

    /// <summary>
    /// Result of test script execution.
    /// </summary>
    public class TestScriptResult
    {
        public string TestCaseId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalSteps { get; set; }
        public bool OverallSuccess { get; set; } = true;
        public List<KeywordTestStep> PassedSteps { get; } = new();
        public List<KeywordTestStep> FailedSteps { get; } = new();
        public List<KeywordTestStep> ExpectedFailures { get; } = new();
        public string? ErrorMessage { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QualiFrame.Framework.KeywordDriven;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Tests.Tests.Examples
{
    /// <summary>
    /// Example test demonstrating keyword-driven testing.
    /// </summary>
    [TestFixture]
    public class KeywordDrivenTest
    {
        [Test]
        public void ExecuteKeywordTestFromExcel()
        {
            // Example: Execute keyword-driven test from Excel file
            var testScript = new TestScript();
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "KeywordTests.xlsx");
            var sheetName = "TestSteps";
            var testCaseId = "TC001";

            if (!File.Exists(filePath))
            {
                Assert.Warn($"Excel file not found: {filePath}. Skipping test.");
                return;
            }

            var result = testScript.ExecuteTestFromExcel(filePath, sheetName, testCaseId);

            Assert.That(result.OverallSuccess, Is.True, 
                $"Test failed. Failed steps: {result.FailedSteps.Count}, Error: {result.ErrorMessage}");
            Assert.That(result.TotalSteps, Is.GreaterThan(0), "No test steps were executed");
            Logger.Information("Keyword-driven test completed. Passed: {Passed}, Failed: {Failed}", 
                result.PassedSteps.Count, result.FailedSteps.Count);
        }

        [Test]
        public void ExecuteKeywordTestProgrammatically()
        {
            // Example: Execute keyword-driven test programmatically
            var testScript = new TestScript();
            var steps = new List<KeywordTestStep>
            {
                new KeywordTestStep
                {
                    StepNumber = 1,
                    KeywordName = "Navigate",
                    Description = "Navigate to application",
                    Parameters = new Dictionary<string, object>
                    {
                        { "URL", "/" },
                        { "UseBaseUrl", true }
                    }
                },
                new KeywordTestStep
                {
                    StepNumber = 2,
                    KeywordName = "Type",
                    Description = "Enter username",
                    Parameters = new Dictionary<string, object>
                    {
                        { "LocatorType", "id" },
                        { "LocatorValue", "user-name" },
                        { "Text", "standard_user" }
                    }
                },
                new KeywordTestStep
                {
                    StepNumber = 3,
                    KeywordName = "Type",
                    Description = "Enter password",
                    Parameters = new Dictionary<string, object>
                    {
                        { "LocatorType", "id" },
                        { "LocatorValue", "password" },
                        { "Text", "secret_sauce" }
                    }
                },
                new KeywordTestStep
                {
                    StepNumber = 4,
                    KeywordName = "Click",
                    Description = "Click login button",
                    Parameters = new Dictionary<string, object>
                    {
                        { "LocatorType", "id" },
                        { "LocatorValue", "login-button" }
                    }
                },
                new KeywordTestStep
                {
                    StepNumber = 5,
                    KeywordName = "VerifyText",
                    Description = "Verify page title",
                    Parameters = new Dictionary<string, object>
                    {
                        { "LocatorType", "class" },
                        { "LocatorValue", "title" },
                        { "ExpectedText", "Products" },
                        { "MatchType", "contains" }
                    }
                }
            };

            var result = testScript.ExecuteTest(steps, "TC_Programmatic_001");

            Assert.That(result.OverallSuccess, Is.True, 
                $"Test failed. Failed steps: {result.FailedSteps.Count}");
            Assert.That(result.PassedSteps.Count, Is.EqualTo(5), "All steps should pass");
        }
    }
}

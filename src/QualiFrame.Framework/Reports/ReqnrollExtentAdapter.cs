using AventStack.ExtentReports;
using Reqnroll;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Reports
{
    /// <summary>
    /// Reqnroll adapter for ExtentReports integration.
    /// Provides automatic step tracking similar to Java's ExtentCucumberAdapter.
    /// </summary>
    public static class ReqnrollExtentAdapter
    {
        /// <summary>
        /// Gets the current step from ExtentReports for the current thread.
        /// Similar to Java's ExtentCucumberAdapter.getCurrentStep().
        /// </summary>
        public static ExtentTest? GetCurrentStep()
        {
            try
            {
                // Access the current step from ExtentReportManager's thread-local storage
                // This requires making the step accessible, but we'll use reflection or a public getter
                // For now, we'll use a workaround by checking if we're in a step context
                return ExtentReportManager.GetCurrentStep();
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to get current ExtentReports step: {Error}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Logs a step to ExtentReports with automatic status mapping.
        /// Similar to Java's updateExtentReport method.
        /// </summary>
        public static void LogStep(string stepName, string stepDescription, string status)
        {
            try
            {
                var currentStep = GetCurrentStep();
                if (currentStep != null)
                {
                    var mappedStatus = ExtentReportStatusMapper.MapStatus(status);
                    currentStep.Log(mappedStatus, $"{stepName} : {stepDescription}");
                }
                else
                {
                    Logger.Warning("Cannot log to ExtentReports: No current step available. Step: {StepName}", stepName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to log step to ExtentReports: {StepName}", stepName);
            }
        }

        /// <summary>
        /// Logs a step to ExtentReports with screenshot.
        /// </summary>
        public static void LogStepWithScreenshot(string stepName, string stepDescription, string status, string screenshotPath)
        {
            try
            {
                var currentStep = GetCurrentStep();
                if (currentStep != null)
                {
                    var mappedStatus = ExtentReportStatusMapper.MapStatus(status);
                    currentStep.Log(mappedStatus, $"{stepName} : {stepDescription}");
                    ExtentReportManager.AddScreenshot(screenshotPath, stepName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to log step with screenshot to ExtentReports: {StepName}", stepName);
            }
        }

        /// <summary>
        /// Logs a step to ExtentReports with boolean result.
        /// </summary>
        public static void LogStep(string stepName, string stepDescription, bool passed)
        {
            var status = passed ? "PASS" : "FAIL";
            LogStep(stepName, stepDescription, status);
        }
    }
}

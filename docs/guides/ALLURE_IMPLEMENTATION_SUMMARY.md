# Allure Reporting Implementation Summary

## Overview

This document summarizes the Allure reporting implementation in QualiFrame C# NUnit Framework v3.1, including the challenges encountered and the solution implemented.

## Problem Statement

Allure.NUnit does not automatically generate test results for Reqnroll-generated NUnit tests. When tests are executed, the `Results/AllureResults` directory remains empty, preventing Allure report generation.

## Root Cause

1. **Reqnroll Test Generation**: Reqnroll generates NUnit test methods dynamically from `.feature` files. These generated tests are not recognized by Allure.NUnit's automatic integration.

2. **Allure.NUnit Limitations**: Allure.NUnit is designed to work with standard NUnit tests that use Allure attributes (e.g., `[AllureEpic]`, `[AllureFeature]`). It doesn't automatically capture results from Reqnroll-generated test methods.

3. **Missing Result Files**: Allure.NUnit expects result JSON files to be written to the results directory, but these files are not generated automatically for Reqnroll scenarios.

## Solution Implemented

### 1. Manual Result Writing

The framework now manually writes Allure result JSON files using the `AllureLifecycle` API and direct file writing:

- **BeforeScenario Hook**: Creates Allure test case with unique UUID, feature labels, and tags
- **AfterScenario Hook**: Updates test status, writes result JSON file, and creates container file
- **BeforeStep/AfterStep Hooks**: Tracks step execution and adds failure screenshots

### 2. Configuration Files

#### `allureConfig.json`
Created at `src/QualiFrame.Tests/allureConfig.json`:
```json
{
  "allure": {
    "directory": "Results/AllureResults",
    "links": [
      "https://github.com/allure-framework/allure2/issues/{issue}",
      "https://example.com/tms/{tms}"
    ]
  }
}
```

- Automatically copied to output directory during build
- Dynamically updated with absolute path at runtime
- Referenced via `ALLURE_CONFIG` environment variable

#### `appsettings.json`
Configuration section:
```json
"AllureReport": {
  "Enable": true,
  "ResultsPath": "Results/AllureResults"
}
```

### 3. Environment Variables

The framework sets the following environment variables:

- **`ALLURE_RESULTS_DIRECTORY`**: Absolute path to the results directory (takes precedence over config file)
- **`ALLURE_CONFIG`**: Path to the `allureConfig.json` file

### 4. Implementation Details

#### BeforeTestRun Hook
- Resolves absolute path for results directory
- Sets `ALLURE_RESULTS_DIRECTORY` environment variable
- Creates results directory if it doesn't exist
- Verifies directory is writable
- Updates `allureConfig.json` with absolute path
- Sets `ALLURE_CONFIG` environment variable

#### BeforeScenario Hook
- Checks if Allure reporting is enabled
- Gets feature title from `FeatureContext`
- Creates `TestResult` object with:
  - Unique UUID
  - Test name and full name
  - Feature and tag labels
  - Start timestamp
- Calls `AllureLifecycle.Instance.StartTestCase()`
- Stores UUID and feature title in scenario context

#### AfterScenario Hook
- Retrieves test UUID from scenario context
- Updates test status (passed/failed)
- Adds failure details if test failed
- Calls `AllureLifecycle.Instance.UpdateTestCase()` and `StopTestCase()`
- Manually writes result JSON file using `Newtonsoft.Json`
- Creates container JSON file for feature grouping
- Verifies files were written successfully

#### BeforeStep/AfterStep Hooks
- Tracks step execution using `AllureLifecycle.Instance.StartStep()` and `StopStep()`
- Adds failure screenshots to Allure reports
- Updates step status based on test errors

### 5. File Format

#### Result File: `{uuid}-result.json`
Contains:
- `uuid`: Unique test identifier
- `name`: Test/scenario name
- `fullName`: Feature.TestName
- `status`: passed/failed/none
- `start`/`stop`: Timestamps
- `labels`: Feature, tags, etc.
- `statusDetails`: Error message and stack trace (if failed)
- `attachments`: Screenshots and other attachments

#### Container File: `{uuid}-container.json`
Contains:
- `uuid`: Unique container identifier
- `name`: Feature name
- `children`: Array of test UUIDs
- `befores`/`afters`: Empty arrays

### 6. Serialization

Uses `Newtonsoft.Json` for JSON serialization:
- Better compatibility with Allure format
- Already available via RestSharp dependencies
- Handles Allure-specific data structures correctly

### 7. Logging and Debugging

Extensive logging added for troubleshooting:
- Console output for immediate visibility
- Logger output for detailed logs
- File path verification
- Error messages with stack traces
- Configuration status logging

## Files Modified

1. **`src/QualiFrame.Tests/StepDefinitions/Hooks.cs`**:
   - Enhanced `BeforeTestRun` with Allure configuration
   - Enhanced `BeforeScenario` with Allure test case initialization
   - Enhanced `AfterScenario` with manual result writing
   - Enhanced `BeforeStep`/`AfterStep` with Allure step tracking

2. **`src/QualiFrame.Tests/allureConfig.json`** (NEW):
   - Allure configuration file
   - Specifies results directory and links

3. **`src/QualiFrame.Tests/QualiFrame.Tests.csproj`**:
   - Added `allureConfig.json` to be copied to output directory
   - Updated Allure.NUnit package version to 2.14.1

4. **`src/QualiFrame.Tests/Configuration/appsettings.json`**:
   - Already had Allure configuration section

## Usage

### Enable/Disable Allure Reporting

Edit `appsettings.json`:
```json
"AllureReport": {
  "Enable": true,  // Set to false to disable
  "ResultsPath": "Results/AllureResults"
}
```

### Generate Allure Report

After test execution, generate the HTML report:
```bash
allure generate Results/AllureResults -o Results/AllureReport --clean
```

### Open Allure Report

```bash
allure open Results/AllureReport
```

### Automatic Report Generation

The framework attempts to automatically generate the Allure report after test execution if:
- Allure CLI is available in PATH
- Allure reporting is enabled
- Result files exist in the results directory

## Troubleshooting

### Results Directory is Empty

1. **Check Configuration**:
   - Verify `AllureReport:Enable` is `true` in `appsettings.json`
   - Check console output for `[BEFORE_TEST_RUN] Allure reporting enabled: True`

2. **Check Hooks Execution**:
   - Look for `[BEFORE_SCENARIO]` and `[AFTER_SCENARIO]` messages in console
   - Verify hooks are being called for your tests

3. **Check Environment Variables**:
   - Console should show `[INFO] ALLURE_RESULTS_DIRECTORY set to: ...`
   - Verify the path is correct and writable

4. **Check File Writing**:
   - Look for `[INFO] ✅ Allure result written to: ...` messages
   - Verify files are being created in the expected location

5. **Check Logs**:
   - Review `Results/Logs/` for detailed error messages
   - Search for "Allure" or "ERROR" entries

### Common Issues

1. **Allure CLI Not Found**:
   - Install Allure CLI: `scoop install allure` (Windows) or `npm install -g allure-commandline`
   - Or manually generate reports using the command above

2. **Permission Errors**:
   - Ensure the results directory is writable
   - Check file permissions on the results directory

3. **Path Resolution Issues**:
   - Verify the resolved path in console output
   - Check that the path exists and is accessible

4. **Missing Configuration File**:
   - Ensure `allureConfig.json` is copied to output directory
   - Check `bin/Debug/net8.0/allureConfig.json` or `bin/Release/net8.0/allureConfig.json`

## Technical Notes

### Why Manual Writing?

Allure.NUnit's automatic integration doesn't work with Reqnroll-generated tests because:
- Reqnroll generates test methods at compile time
- Allure.NUnit hooks into NUnit's test execution pipeline
- The generated test methods don't have Allure attributes
- Allure.NUnit doesn't recognize Reqnroll's test structure

### Alternative Approaches Considered

1. **Allure.Reqnroll Package**: Not available or compatible with Reqnroll 3.0
2. **Custom Allure Adapter**: Would require significant development effort
3. **Manual Writing**: Chosen as the most practical solution

### Future Improvements

Potential enhancements:
- Support for Allure attachments (screenshots, logs)
- Integration with Allure TestOps
- Custom Allure labels and links
- Test history tracking
- Parallel execution support improvements

## References

- [Allure Framework Documentation](https://docs.qameta.io/allure/)
- [Allure.NUnit Documentation](https://allurereport.org/docs/nunit-configuration/)
- [Reqnroll Documentation](https://docs.reqnroll.net/)
- [Allure JSON Format Specification](https://github.com/allure-framework/allure2/blob/master/allure-model/src/main/java/io/qameta/allure/model/TestResult.java)


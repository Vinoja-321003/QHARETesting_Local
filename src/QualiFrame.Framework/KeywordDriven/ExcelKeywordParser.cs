using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QualiFrame.Framework.Data.Repositories;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Parser for reading keyword-driven tests from Excel files.
    /// </summary>
    public class ExcelKeywordParser
    {
        /// <summary>
        /// Parses keyword test steps from an Excel file.
        /// </summary>
        public List<KeywordTestStep> ParseTestSteps(string filePath, string sheetName, string? testCaseId = null)
        {
            var steps = new List<KeywordTestStep>();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(sheetName);

                if (worksheet == null || worksheet.IsEmpty())
                {
                    throw new FileNotFoundException($"Sheet '{sheetName}' not found or is empty in Excel file: {filePath}");
                }

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    throw new FileNotFoundException($"No used range found in sheet '{sheetName}' in Excel file: {filePath}");
                }

                var headerRow = 1;
                var headerMap = new Dictionary<string, int>();
                var lastColumn = range.LastColumn().ColumnNumber();

                for (int col = 1; col <= lastColumn; col++)
                {
                    var headerValue = worksheet.Cell(headerRow, col).GetString()?.Trim();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        headerMap[headerValue.ToLowerInvariant()] = col;
                    }
                }

                if (!headerMap.ContainsKey("step") && !headerMap.ContainsKey("stepnumber"))
                {
                    throw new InvalidOperationException("Excel file must contain 'Step' or 'StepNumber' column");
                }

                if (!headerMap.ContainsKey("keyword"))
                {
                    throw new InvalidOperationException("Excel file must contain 'Keyword' column");
                }

                var lastRow = range.LastRow().RowNumber();

                for (int row = headerRow + 1; row <= lastRow; row++)
                {
                    var stepNumberCol = headerMap.ContainsKey("stepnumber") 
                        ? headerMap["stepnumber"] 
                        : headerMap["step"];
                    var keywordCol = headerMap["keyword"];
                    var testCaseIdCol = headerMap.ContainsKey("testcaseid") ? headerMap["testcaseid"] : -1;

                    if (testCaseId != null && testCaseIdCol > 0)
                    {
                        var rowTestCaseId = worksheet.Cell(row, testCaseIdCol).GetString()?.Trim();
                        if (rowTestCaseId != testCaseId)
                        {
                            continue;
                        }
                    }

                    var stepNumberText = worksheet.Cell(row, stepNumberCol).GetString()?.Trim();
                    if (string.IsNullOrEmpty(stepNumberText) || !int.TryParse(stepNumberText, out var stepNumber))
                    {
                        continue;
                    }

                    var keywordName = worksheet.Cell(row, keywordCol).GetString()?.Trim();
                    if (string.IsNullOrEmpty(keywordName))
                    {
                        continue;
                    }

                    var step = new KeywordTestStep
                    {
                        StepNumber = stepNumber,
                        KeywordName = keywordName
                    };

                    if (headerMap.ContainsKey("description"))
                    {
                        step.Description = worksheet.Cell(row, headerMap["description"]).GetString()?.Trim() ?? string.Empty;
                    }

                    if (headerMap.ContainsKey("expectedtofail"))
                    {
                        var expectedToFailText = worksheet.Cell(row, headerMap["expectedtofail"]).GetString()?.Trim();
                        step.ExpectedToFail = expectedToFailText?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
                    }

                    foreach (var header in headerMap)
                    {
                        var headerName = header.Key;
                        if (headerName != "step" && headerName != "stepnumber" && headerName != "keyword" 
                            && headerName != "description" && headerName != "expectedtofail" && headerName != "testcaseid")
                        {
                            var value = worksheet.Cell(row, header.Value).GetString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                step.Parameters[headerName] = value;
                            }
                        }
                    }

                    steps.Add(step);
                }

                Logger.Information("Parsed {Count} keyword test steps from {FilePath}, sheet: {SheetName}", 
                    steps.Count, filePath, sheetName);

                return steps.OrderBy(s => s.StepNumber).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error parsing keyword test steps from Excel file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Gets all test case IDs from an Excel file.
        /// </summary>
        public List<string> GetTestCaseIds(string filePath, string sheetName)
        {
            var testCaseIds = new HashSet<string>();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(sheetName);

                if (worksheet == null || worksheet.IsEmpty())
                {
                    return new List<string>();
                }

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    return new List<string>();
                }

                var headerRow = 1;
                var headerMap = new Dictionary<string, int>();
                var lastColumn = range.LastColumn().ColumnNumber();

                for (int col = 1; col <= lastColumn; col++)
                {
                    var headerValue = worksheet.Cell(headerRow, col).GetString()?.Trim();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        headerMap[headerValue.ToLowerInvariant()] = col;
                    }
                }

                if (!headerMap.ContainsKey("testcaseid"))
                {
                    return new List<string>();
                }

                var testCaseIdCol = headerMap["testcaseid"];
                var lastRow = range.LastRow().RowNumber();

                for (int row = headerRow + 1; row <= lastRow; row++)
                {
                    var testCaseId = worksheet.Cell(row, testCaseIdCol).GetString()?.Trim();
                    if (!string.IsNullOrEmpty(testCaseId))
                    {
                        testCaseIds.Add(testCaseId);
                    }
                }

                return testCaseIds.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting test case IDs from Excel file: {FilePath}", filePath);
                return new List<string>();
            }
        }
    }
}

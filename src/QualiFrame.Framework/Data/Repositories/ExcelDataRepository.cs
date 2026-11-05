using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Data.Repositories
{
    /// <summary>
    /// Repository for Excel data access.
    /// </summary>
    public class ExcelDataRepository : IDataRepository
    {
        private readonly string _defaultDataTable;

        public ExcelDataRepository(string? defaultDataTable = null)
        {
            _defaultDataTable = defaultDataTable ?? "WebModularScenario";
        }

        public Dictionary<string, object> GetTestData(string testCaseId, string? source = null)
        {
            var excelFile = source ?? $"Data/{_defaultDataTable}.xlsx";
            var excelPath = Path.Combine(Directory.GetCurrentDirectory(), excelFile);

            if (!File.Exists(excelPath))
            {
                Logger.Warning("Excel file not found: {Path}. Falling back to CSV.", excelPath);
                throw new TestDataException(
                    $"Excel file not found: {excelPath}",
                    testCaseId: testCaseId,
                    dataSource: excelFile);
            }

            try
            {
                using var workbook = new XLWorkbook(excelPath);
                var worksheet = workbook.Worksheet(1);

                if (worksheet == null || worksheet.IsEmpty())
                {
                    throw new TestDataException(
                        "Worksheet not found or is empty in Excel file",
                        testCaseId: testCaseId,
                        dataSource: excelFile);
                }

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    throw new TestDataException(
                        "No used range found in Excel worksheet",
                        testCaseId: testCaseId,
                        dataSource: excelFile);
                }

                var headerRow = 1;
                var headers = new List<string>();
                var lastColumn = range.LastColumn().ColumnNumber();

                for (int col = 1; col <= lastColumn; col++)
                {
                    var header = worksheet.Cell(headerRow, col).GetString();
                    if (!string.IsNullOrEmpty(header))
                    {
                        headers.Add(header);
                    }
                }

                var testCaseColumn = headers.IndexOf("TCID") + 1;
                if (testCaseColumn == 0)
                {
                    testCaseColumn = headers.IndexOf("TestCaseId") + 1;
                }

                if (testCaseColumn == 0)
                {
                    throw new TestDataException(
                        "TCID or TestCaseId column not found",
                        testCaseId: testCaseId,
                        dataSource: excelFile);
                }

                Dictionary<string, object> testData = new();
                var lastRow = range.LastRow().RowNumber();

                for (int row = 2; row <= lastRow; row++)
                {
                    var cellValue = worksheet.Cell(row, testCaseColumn).GetString();
                    if (cellValue == testCaseId)
                    {
                        for (int col = 0; col < headers.Count; col++)
                        {
                            var value = worksheet.Cell(row, col + 1).Value;
                            testData[headers[col]] = value.ToString() ?? string.Empty;
                        }
                        Logger.Information("Test data retrieved for {TestCaseId} from Excel", testCaseId);
                        return testData;
                    }
                }

                throw new TestDataException(
                    $"Test case ID '{testCaseId}' not found in Excel file",
                    testCaseId: testCaseId,
                    dataSource: excelFile);
            }
            catch (TestDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading Excel file: {Path}", excelPath);
                throw new TestDataException(
                    $"Error reading Excel file: {ex.Message}",
                    ex,
                    testCaseId: testCaseId,
                    dataSource: excelFile);
            }
        }

        public List<Dictionary<string, object>> GetAllTestData(string? source = null)
        {
            var excelFile = source ?? $"Data/{_defaultDataTable}.xlsx";
            var excelPath = Path.Combine(Directory.GetCurrentDirectory(), excelFile);

            if (!File.Exists(excelPath))
            {
                throw new TestDataException(
                    $"Excel file not found: {excelPath}",
                    dataSource: excelFile);
            }

            var allData = new List<Dictionary<string, object>>();

            try
            {
                using var workbook = new XLWorkbook(excelPath);
                var worksheet = workbook.Worksheet(1);

                if (worksheet == null || worksheet.IsEmpty())
                {
                    return allData;
                }

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    return allData;
                }

                var headerRow = 1;
                var headers = new List<string>();
                var lastColumn = range.LastColumn().ColumnNumber();
                var lastRow = range.LastRow().RowNumber();

                for (int col = 1; col <= lastColumn; col++)
                {
                    var header = worksheet.Cell(headerRow, col).GetString();
                    if (!string.IsNullOrEmpty(header))
                    {
                        headers.Add(header);
                    }
                }

                for (int row = 2; row <= lastRow; row++)
                {
                    Dictionary<string, object> rowData = new();
                    for (int col = 0; col < headers.Count; col++)
                    {
                        var value = worksheet.Cell(row, col + 1).Value;
                        rowData[headers[col]] = value.ToString() ?? string.Empty;
                    }
                    allData.Add(rowData);
                }

                return allData;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading all data from Excel file: {Path}", excelPath);
                throw new TestDataException(
                    $"Error reading Excel file: {ex.Message}",
                    ex,
                    dataSource: excelFile);
            }
        }

        public bool TestDataExists(string testCaseId, string? source = null)
        {
            try
            {
                GetTestData(testCaseId, source);
                return true;
            }
            catch (TestDataException)
            {
                return false;
            }
        }
    }
}

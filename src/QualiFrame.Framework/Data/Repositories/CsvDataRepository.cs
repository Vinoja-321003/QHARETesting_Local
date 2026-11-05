using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;
using System.Globalization;

namespace QualiFrame.Framework.Data.Repositories
{
    /// <summary>
    /// Repository for CSV data access.
    /// </summary>
    public class CsvDataRepository : IDataRepository
    {
        public Dictionary<string, object> GetTestData(string testCaseId, string? source = null)
        {
            var csvFile = source ?? "Data/TestData.csv";
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), csvFile);

            if (!File.Exists(csvPath))
            {
                throw new TestDataException(
                    $"CSV file not found: {csvPath}",
                    testCaseId: testCaseId,
                    dataSource: csvFile);
            }

            try
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim
                });

                var records = csv.GetRecords<dynamic>().ToList();

                foreach (var record in records)
                {
                    var recordDict = (IDictionary<string, object>)record;
                    var tcId = recordDict.ContainsKey("TCID") 
                        ? recordDict["TCID"]?.ToString() 
                        : recordDict.ContainsKey("TestCaseId") 
                            ? recordDict["TestCaseId"]?.ToString() 
                            : null;

                    if (tcId == testCaseId)
                    {
                        var testData = new Dictionary<string, object>();
                        foreach (var kvp in recordDict)
                        {
                            testData[kvp.Key] = kvp.Value ?? string.Empty;
                        }
                        Logger.Information("Test data retrieved for {TestCaseId} from CSV", testCaseId);
                        return testData;
                    }
                }

                throw new TestDataException(
                    $"Test case ID '{testCaseId}' not found in CSV file",
                    testCaseId: testCaseId,
                    dataSource: csvFile);
            }
            catch (TestDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading CSV file: {Path}", csvPath);
                throw new TestDataException(
                    $"Error reading CSV file: {ex.Message}",
                    ex,
                    testCaseId: testCaseId,
                    dataSource: csvFile);
            }
        }

        public List<Dictionary<string, object>> GetAllTestData(string? source = null)
        {
            var csvFile = source ?? "Data/TestData.csv";
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), csvFile);

            if (!File.Exists(csvPath))
            {
                throw new TestDataException(
                    $"CSV file not found: {csvPath}",
                    dataSource: csvFile);
            }

            var allData = new List<Dictionary<string, object>>();

            try
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim
                });

                var records = csv.GetRecords<dynamic>().ToList();

                foreach (var record in records)
                {
                    var recordDict = (IDictionary<string, object>)record;
                    var rowData = new Dictionary<string, object>();
                    foreach (var kvp in recordDict)
                    {
                        rowData[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                    allData.Add(rowData);
                }

                return allData;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading all data from CSV file: {Path}", csvPath);
                throw new TestDataException(
                    $"Error reading CSV file: {ex.Message}",
                    ex,
                    dataSource: csvFile);
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

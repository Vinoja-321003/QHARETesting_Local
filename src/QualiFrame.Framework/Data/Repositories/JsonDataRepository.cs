using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Data.Repositories
{
    /// <summary>
    /// Repository for JSON data access.
    /// </summary>
    public class JsonDataRepository : IDataRepository
    {
        public Dictionary<string, object> GetTestData(string testCaseId, string? source = null)
        {
            var jsonFile = source ?? "Data/TestData.json";
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), jsonFile);

            if (!File.Exists(jsonPath))
            {
                throw new TestDataException(
                    $"JSON file not found: {jsonPath}",
                    testCaseId: testCaseId,
                    dataSource: jsonFile);
            }

            try
            {
                var jsonContent = File.ReadAllText(jsonPath);
                var jsonObject = JObject.Parse(jsonContent);

                var testData = FindTestData(jsonObject, testCaseId);

                if (testData != null)
                {
                    Logger.Information("Test data retrieved for {TestCaseId} from JSON", testCaseId);
                    return testData.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                }

                throw new TestDataException(
                    $"Test case ID '{testCaseId}' not found in JSON file",
                    testCaseId: testCaseId,
                    dataSource: jsonFile);
            }
            catch (TestDataException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                Logger.Error(ex, "Invalid JSON format in file: {Path}", jsonPath);
                throw new TestDataException(
                    $"Invalid JSON format: {ex.Message}",
                    ex,
                    testCaseId: testCaseId,
                    dataSource: jsonFile);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading JSON file: {Path}", jsonPath);
                throw new TestDataException(
                    $"Error reading JSON file: {ex.Message}",
                    ex,
                    testCaseId: testCaseId,
                    dataSource: jsonFile);
            }
        }

        public List<Dictionary<string, object>> GetAllTestData(string? source = null)
        {
            var jsonFile = source ?? "Data/TestData.json";
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), jsonFile);

            if (!File.Exists(jsonPath))
            {
                throw new TestDataException(
                    $"JSON file not found: {jsonPath}",
                    dataSource: jsonFile);
            }

            var allData = new List<Dictionary<string, object>>();

            try
            {
                var jsonContent = File.ReadAllText(jsonPath);
                var jsonObject = JObject.Parse(jsonContent);

                foreach (var property in jsonObject.Properties())
                {
                    if (property.Value is JArray array)
                    {
                        foreach (var item in array)
                        {
                            if (item is JObject obj)
                            {
                                allData.Add(obj.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>());
                            }
                        }
                    }
                }

                return allData;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading all data from JSON file: {Path}", jsonPath);
                throw new TestDataException(
                    $"Error reading JSON file: {ex.Message}",
                    ex,
                    dataSource: jsonFile);
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

        private JObject? FindTestData(JObject jsonObject, string testCaseId)
        {
            foreach (var property in jsonObject.Properties())
            {
                if (property.Value is JArray array)
                {
                    foreach (var item in array)
                    {
                        if (item is JObject obj)
                        {
                            var tcId = obj["TCID"]?.ToString() 
                                ?? obj["TestCaseId"]?.ToString() 
                                ?? obj["testCaseId"]?.ToString();
                            
                            if (tcId == testCaseId)
                            {
                                return obj;
                            }
                        }
                    }
                }
            }

            foreach (var property in jsonObject.Properties())
            {
                if (property.Value is JObject obj)
                {
                    var result = FindTestData(obj, testCaseId);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}

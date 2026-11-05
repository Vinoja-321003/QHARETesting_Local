using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Data.Repositories
{
    /// <summary>
    /// SQL Server test-data repository. Opt-in via DataRepositoryFactory ("SQL" / "SQLSERVER" / "MSSQL").
    /// Does not run unless SQL is requested; Excel remains the factory default.
    /// </summary>
    public class SqlDataRepository : IDataRepository
    {
        private static readonly Regex IdentifierPattern = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private readonly TestConfiguration _config;

        public SqlDataRepository()
        {
            _config = TestConfiguration.Instance;
        }

        public Dictionary<string, object> GetTestData(string testCaseId, string? source = null)
        {
            var rows = GetAllTestData(source);
            var match = rows.FirstOrDefault(row => MatchesTestCaseId(row, testCaseId));
            if (match == null)
            {
                throw new TestDataException(
                    $"Test case ID '{testCaseId}' not found in SQL table '{ResolveTableName(source)}'",
                    testCaseId: testCaseId,
                    dataSource: ResolveTableName(source));
            }

            Logger.Information("Test data retrieved for {TestCaseId} from SQL table {Table}", testCaseId, ResolveTableName(source));
            return match;
        }

        public List<Dictionary<string, object>> GetAllTestData(string? source = null)
        {
            var tableName = ResolveTableName(source);
            var quotedTable = QuoteTableName(tableName);
            var connectionString = BuildConnectionString();

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                using var command = new SqlCommand($"SELECT * FROM {quotedTable}", connection)
                {
                    CommandTimeout = _config.DatabaseCommandTimeout
                };

                using var reader = command.ExecuteReader();
                var rows = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i);
                        row[reader.GetName(i)] = value;
                    }
                    rows.Add(row);
                }

                Logger.Debug("Loaded {Count} SQL test-data row(s) from {Table}", rows.Count, tableName);
                return rows;
            }
            catch (TestDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading SQL test data from table {Table}", tableName);
                throw new TestDataException(
                    $"Error reading SQL table '{tableName}': {ex.Message}",
                    ex,
                    dataSource: tableName);
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

        private string ResolveTableName(string? source)
        {
            return string.IsNullOrWhiteSpace(source) ? _config.DatabaseDefaultTable : source.Trim();
        }

        private string BuildConnectionString()
        {
            var configured = _config.DatabaseConnectionString;
            if (string.IsNullOrWhiteSpace(configured))
            {
                throw new TestDataException(
                    "SQL data repository requires Environments:QA:DatabaseConnectionString (or an environment override). No connection string is configured.",
                    dataSource: "SQL");
            }

            var builder = new SqlConnectionStringBuilder(configured)
            {
                ConnectTimeout = _config.DatabaseConnectionTimeout
            };
            return builder.ConnectionString;
        }

        private static bool MatchesTestCaseId(Dictionary<string, object> row, string testCaseId)
        {
            foreach (var key in new[] { "TCID", "TestCaseId", "testCaseId" })
            {
                if (row.TryGetValue(key, out var value) &&
                    string.Equals(value?.ToString(), testCaseId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string QuoteTableName(string tableName)
        {
            var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length is < 1 or > 2)
            {
                throw new TestDataException($"Invalid SQL table name '{tableName}'. Use Table or schema.Table.");
            }

            foreach (var part in parts)
            {
                if (!IdentifierPattern.IsMatch(part))
                {
                    throw new TestDataException($"Invalid SQL identifier '{tableName}'.");
                }
            }

            return string.Join('.', parts.Select(part => $"[{part}]"));
        }
    }
}

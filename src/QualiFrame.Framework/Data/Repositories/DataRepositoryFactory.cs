using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Data.Repositories
{
    /// <summary>
    /// Factory for creating data repository instances.
    /// </summary>
    public static class DataRepositoryFactory
    {
        /// <summary>
        /// Creates a data repository based on configuration.
        /// </summary>
        public static IDataRepository CreateRepository(string? sourceType = null)
        {
            var dataSource = sourceType ?? "EXCEL";

            Logger.Debug("Creating data repository for source type: {SourceType}", dataSource);

            return dataSource.ToUpperInvariant() switch
            {
                "EXCEL" => new ExcelDataRepository(),
                "CSV" => new CsvDataRepository(),
                "JSON" => new JsonDataRepository(),
                "SQL" or "SQLSERVER" or "MSSQL" or "DATABASE" => new SqlDataRepository(),
                _ => new ExcelDataRepository()
            };
        }
    }
}

using System.Collections.Generic;

namespace QualiFrame.Framework.Data.Repositories
{
    /// <summary>
    /// Interface for data repository operations.
    /// </summary>
    public interface IDataRepository
    {
        /// <summary>
        /// Gets test data for a test case ID.
        /// </summary>
        Dictionary<string, object> GetTestData(string testCaseId, string? source = null);

        /// <summary>
        /// Gets all test data from a source.
        /// </summary>
        List<Dictionary<string, object>> GetAllTestData(string? source = null);

        /// <summary>
        /// Validates test data exists for a test case ID.
        /// </summary>
        bool TestDataExists(string testCaseId, string? source = null);
    }
}

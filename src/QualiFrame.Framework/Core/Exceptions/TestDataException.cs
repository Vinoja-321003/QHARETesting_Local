using System;

namespace QualiFrame.Framework.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when test data operations fail.
    /// </summary>
    public class TestDataException : FrameworkException
    {
        public string? DataSource { get; }
        public string? TestCaseId { get; }

        public TestDataException(string message) : base(message)
        {
        }

        public TestDataException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public TestDataException(string message, string? testCaseId = null, string? dataSource = null) 
            : base(message, testCaseId)
        {
            TestCaseId = testCaseId;
            DataSource = dataSource;
        }

        public TestDataException(string message, Exception innerException, string? testCaseId = null, string? dataSource = null) 
            : base(message, innerException, testCaseId)
        {
            TestCaseId = testCaseId;
            DataSource = dataSource;
        }
    }
}

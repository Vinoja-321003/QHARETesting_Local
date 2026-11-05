using System;

namespace QualiFrame.Framework.Core.Exceptions
{
    /// <summary>
    /// Base exception class for all framework exceptions.
    /// </summary>
    public class FrameworkException : Exception
    {
        public string? TestId { get; }
        public string? Context { get; }
        public DateTime Timestamp { get; }

        public FrameworkException(string message) : base(message)
        {
            Timestamp = DateTime.UtcNow;
        }

        public FrameworkException(string message, Exception innerException) 
            : base(message, innerException)
        {
            Timestamp = DateTime.UtcNow;
        }

        public FrameworkException(string message, string? testId = null, string? context = null) 
            : base(message)
        {
            TestId = testId;
            Context = context;
            Timestamp = DateTime.UtcNow;
        }

        public FrameworkException(string message, Exception innerException, string? testId = null, string? context = null) 
            : base(message, innerException)
        {
            TestId = testId;
            Context = context;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            var baseString = base.ToString();
            var additionalInfo = $"TestId: {TestId ?? "N/A"}, Context: {Context ?? "N/A"}, Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC";
            return $"{baseString}\nAdditional Info: {additionalInfo}";
        }
    }
}

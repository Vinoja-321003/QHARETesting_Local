using System;

namespace QualiFrame.Framework.Core.Exceptions
{
    /// <summary>
    /// Exception that indicates an operation can be retried.
    /// </summary>
    public class RetryableException : FrameworkException
    {
        public int RetryCount { get; }
        public int MaxRetries { get; }
        public TimeSpan? RetryDelay { get; }

        public RetryableException(string message) : base(message)
        {
        }

        public RetryableException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public RetryableException(string message, int retryCount, int maxRetries, TimeSpan? retryDelay = null, string? testId = null) 
            : base(message, testId)
        {
            RetryCount = retryCount;
            MaxRetries = maxRetries;
            RetryDelay = retryDelay;
        }

        public RetryableException(string message, Exception innerException, int retryCount, int maxRetries, TimeSpan? retryDelay = null, string? testId = null) 
            : base(message, innerException, testId)
        {
            RetryCount = retryCount;
            MaxRetries = maxRetries;
            RetryDelay = retryDelay;
        }

        public bool CanRetry => RetryCount < MaxRetries;
    }
}

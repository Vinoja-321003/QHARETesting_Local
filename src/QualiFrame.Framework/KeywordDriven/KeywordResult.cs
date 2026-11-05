namespace QualiFrame.Framework.KeywordDriven
{
    /// <summary>
    /// Result of keyword execution.
    /// </summary>
    public class KeywordResult
    {
        /// <summary>
        /// Gets or sets whether the keyword execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the result data (e.g., element text, attribute value).
        /// </summary>
        public object? ResultData { get; set; }

        /// <summary>
        /// Gets or sets the exception if execution failed.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static KeywordResult Pass(string message = "", object? data = null)
        {
            return new KeywordResult
            {
                Success = true,
                Message = message,
                ResultData = data
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static KeywordResult Fail(string message, Exception? exception = null)
        {
            return new KeywordResult
            {
                Success = false,
                Message = message,
                Exception = exception
            };
        }
    }
}

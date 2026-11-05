using System;

namespace QualiFrame.Framework.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when WebDriver operations fail.
    /// </summary>
    public class WebDriverException : FrameworkException
    {
        public string? Browser { get; }
        public string? DriverUrl { get; }

        public WebDriverException(string message) : base(message)
        {
        }

        public WebDriverException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public WebDriverException(string message, string? browser = null, string? driverUrl = null, string? testId = null) 
            : base(message, testId)
        {
            Browser = browser;
            DriverUrl = driverUrl;
        }

        public WebDriverException(string message, Exception innerException, string? browser = null, string? driverUrl = null, string? testId = null) 
            : base(message, innerException, testId)
        {
            Browser = browser;
            DriverUrl = driverUrl;
        }
    }
}

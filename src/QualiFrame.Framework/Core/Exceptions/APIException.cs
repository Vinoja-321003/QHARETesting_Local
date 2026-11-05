using System;

namespace QualiFrame.Framework.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when API operations fail.
    /// </summary>
    public class APIException : FrameworkException
    {
        public string? Endpoint { get; }
        public int? StatusCode { get; }
        public string? ResponseBody { get; }

        public APIException(string message) : base(message)
        {
        }

        public APIException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public APIException(string message, string? endpoint = null, int? statusCode = null, string? responseBody = null, string? testId = null) 
            : base(message, testId)
        {
            Endpoint = endpoint;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public APIException(string message, Exception innerException, string? endpoint = null, int? statusCode = null, string? responseBody = null, string? testId = null) 
            : base(message, innerException, testId)
        {
            Endpoint = endpoint;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SwiftClient.AspNetCore
{
    public class SwiftServiceLogger : ISwiftLogger
    {
        private readonly ILogger<SwiftService> _logger;

        private readonly string _authError = "Exception occured: {0} for credentials {1} : {2} on proxy node {3}";
        private readonly string _requestError = "Exception occured: {0} with status code: {1} for request url: {2}";
        private readonly string _unauthorizedError = "Unauthorized request with old token {0} for request url: {1}";

        public SwiftServiceLogger(ILogger<SwiftService> logger)
        {
            _logger = logger;
        }

        public void LogAuthenticationError(Exception ex, string username, string password, string endpoint)
        {
            _logger.LogError(_authError, ex.InnerException != null ? ex.InnerException.Message : ex.Message, username, password, endpoint);
        }

        public void LogRequestError(Exception ex, HttpStatusCode statusCode, string reason, string requestUrl)
        {
            _logger.LogError(_requestError, reason, statusCode.ToString(), requestUrl);
        }

        public void LogUnauthorizedError(string token, string endpoint)
        {
            _logger.LogWarning(_unauthorizedError, token, endpoint);
        }
    }
}

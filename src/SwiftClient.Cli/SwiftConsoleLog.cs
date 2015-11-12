using System;
using System.IO;
using System.Net;

namespace SwiftClient.Cli
{
    public class SwiftConsoleLog : ISwiftLogger
    {
        private string _authError = "Exception occured: {0} for credentials {1} : {2} on proxy node {3}";
        private string _requestError = "Exception occured: {0} with status code: {1} for request url: {2}";
        private string _unauthorizedError = "Unauthorized request with old token {0}";

        public void LogAuthenticationError(Exception ex, string username, string password, string endpoint)
        {
            Logger.LogError(string.Format(_authError, ex.InnerException != null ? ex.InnerException.Message : ex.Message, username, password, endpoint));
        }

        public void LogRequestError(Exception ex, HttpStatusCode statusCode, string reason, string requestUrl)
        {
            Logger.LogError(string.Format(_requestError, reason, statusCode.ToString(), requestUrl));
        }

        public void LogUnauthorizedError(string token, string endpoint)
        {
            Logger.LogError(string.Format(_unauthorizedError, token));
        }
    }
}

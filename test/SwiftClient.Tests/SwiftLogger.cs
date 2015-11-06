using SwiftClient;
using System;
using System.Net;
using System.Diagnostics;

namespace SwiftClient.Tests
{
    public class SwiftLogger : ISwiftLogger
    {
        private string _authError = "Exception occured: {0} for credentials {1} : {2} on proxy node {3}";
        private string _requestError = "Exception occured: {0} with status code: {1} for request url: {2}";
        private string _unauthorizedError = "Unauthorized request with old token {0}";

        public SwiftLogger() { }

        public void LogAuthenticationError(Exception e, string username, string password, string endpoint)
        {
            Trace.WriteLine(string.Format(_authError, e.Message, username, password, endpoint));
        }

        public void LogRequestError(WebException e, HttpStatusCode statusCode, string reason, string requestUrl)
        {
            Trace.WriteLine(string.Format(_requestError, reason, statusCode.ToString(), requestUrl));
        }

        public void LogUnauthorizedError(string token, string endpoint)
        {
            Trace.WriteLine(string.Format(_unauthorizedError, token));
        }
    }

}

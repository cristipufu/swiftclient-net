using System;
using System.Diagnostics;
using System.Net;
using Xunit.Abstractions;

namespace SwiftClient.Test
{
    public class SwiftLogger : ISwiftLogger
    {
        readonly ITestOutputHelper Output;

        private string _authError = "Exception occured: {0} for credentials {1} : {2} on proxy node {3}";
        private string _requestError = "Exception occured: {0} with status code: {1} for request url: {2}";
        private string _unauthorizedError = "Unauthorized request with old token {0}";

        public SwiftLogger(ITestOutputHelper output)
        {
            Output = output;
        }

        public void LogAuthenticationError(Exception e, string username, string password, string endpoint)
        {
            Output.WriteLine(string.Format(_authError, e.InnerException != null ? e.InnerException.Message : e.Message, username, password, endpoint));
        }

        public void LogRequestError(WebException e, HttpStatusCode statusCode, string reason, string requestUrl)
        {
            Output.WriteLine(string.Format(_requestError, reason, statusCode.ToString(), requestUrl));
        }

        public void LogUnauthorizedError(string token, string endpoint)
        {
            Output.WriteLine(string.Format(_unauthorizedError, token));
        }
    }
}

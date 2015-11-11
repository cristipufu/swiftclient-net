using System;
using System.IO;
using System.Net;

namespace SwiftClient.Demo
{
    public class SwiftLogger : ISwiftLogger
    {
        private string _authError = "Exception occured: {0} for credentials {1} : {2} on proxy node {3}";
        private string _requestError = "Exception occured: {0} with status code: {1} for request url: {2}";
        private string _unauthorizedError = "Unauthorized request with old token {0}";

        public SwiftLogger()
        {
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
        }

        public void LogAuthenticationError(Exception e, string username, string password, string endpoint)
        {
            Console.Out.WriteLine(string.Format(_authError, e.Message, username, password, endpoint));
        }

        public void LogRequestError(WebException e, HttpStatusCode statusCode, string reason, string requestUrl)
        {
            Console.Out.WriteLine(string.Format(_requestError, reason, statusCode.ToString(), requestUrl));
        }

        public void LogUnauthorizedError(string token, string endpoint)
        {
            Console.Out.WriteLine(string.Format(_unauthorizedError, token));
        }
    }
}

using System;
using System.Net;

namespace SwiftClient
{
    public interface ISwiftLogger
    {
        void LogAuthenticationError(Exception e, string username, string password, string endpoint);
        void LogRequestError(WebException e, HttpStatusCode statusCode, string reason, string requestUrl);
        void LogUnauthorizedError(string token, string endpoint);
    }
}

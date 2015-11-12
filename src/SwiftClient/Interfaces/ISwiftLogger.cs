using System;
using System.Net;

namespace SwiftClient
{
    public interface ISwiftLogger
    {
        void LogAuthenticationError(Exception ex, string username, string password, string endpoint);
        void LogRequestError(Exception ex, HttpStatusCode statusCode, string reason, string requestUrl);
        void LogUnauthorizedError(string token, string endpoint);
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

using SwiftClient.Extensions;

namespace SwiftClient
{
    public partial class Client : ISwiftClient, IDisposable
    {
        public SwiftRetryManager RetryManager;

        protected ISwiftLogger _logger;
        protected HttpClient _client;
        
        public Client(TimeSpan? timeout = null)
        {
            _client = new HttpClient();
            if (timeout.HasValue)
                _client.Timeout = timeout.Value;
        }

        public Client(SwiftCredentials credentials, TimeSpan? timeout = null) : this(new SwiftAuthManager(credentials), timeout) { }

        public Client(SwiftCredentials credentials, ISwiftLogger logger, TimeSpan? timeout = null) : this(credentials, timeout)
        {
            SetLogger(logger);
        }

        public Client(ISwiftAuthManager authManager, TimeSpan? timeout = null) :this(timeout)
        {
            if (authManager.Authenticate == null)
            {
                authManager.Authenticate = Authenticate;
            }

            RetryManager = new SwiftRetryManager(authManager);
        }

        public Client(ISwiftAuthManager authManager, ISwiftLogger logger, TimeSpan? timeout = null) : this(authManager, timeout)
        {
            SetLogger(logger);
        }

        private void FillRequest(HttpRequestMessage request, SwiftAuthData auth, Dictionary<string, string> headers = null)
        {
            // set headers
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            if (auth != null)
            {
                headers[SwiftHeaderKeys.AuthToken] = auth.AuthToken;
            }

            request.SetHeaders(headers);
        }

        private T GetExceptionResponse<T>(Exception ex, string url) where T : SwiftBaseResponse, new()
        {
            var result = new T();

            var webException = ex as WebException;

            if (webException != null)
            {
                var rsp = ((HttpWebResponse)webException.Response);

                if (rsp != null)
                {
                    result.StatusCode = rsp.StatusCode;
                    result.Reason = rsp.StatusDescription;
                }
            }
            else
            {
                result.StatusCode = HttpStatusCode.BadRequest;
                result.Reason = ex.Message;
            }

            if (_logger != null)
            {
                _logger.LogRequestError(ex, result.StatusCode, result.Reason, url);
            }

            return result;
        }

        private T GetResponse<T>(HttpResponseMessage rsp) where T : SwiftBaseResponse, new()
        {
            var result = new T();
            result.StatusCode = rsp.StatusCode;
            result.Headers = rsp.Headers.ToDictionary();
            result.Reason = rsp.ReasonPhrase;
            result.ContentLength = rsp.Content.Headers.ContentLength ?? 0;
            return result;
        }

        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _client.Dispose();
            }

            disposed = true;
        }
    }
}

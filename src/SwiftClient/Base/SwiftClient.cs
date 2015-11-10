using System;
using System.Collections.Generic;
using System.Net;

using SwiftClient.Extensions;
using System.Net.Http;

namespace SwiftClient
{
    public partial class SwiftClient : ISwiftClient, IDisposable
    {
        protected ISwiftLogger _logger;
        protected SwiftRetryManager _manager;
        protected HttpClient _client = new HttpClient();

        public SwiftClient() { }

        public SwiftClient(SwiftCredentials credentials) : this(new SwiftAuthManager(credentials)) { }

        public SwiftClient(SwiftCredentials credentials, ISwiftLogger logger) : this(credentials)
        {
            SetLogger(logger);
        }

        public SwiftClient(ISwiftAuthManager authManager)
        {
            if (authManager.Authenticate == null)
            {
                authManager.Authenticate = Authenticate;
            }

            _manager = new SwiftRetryManager(authManager);
        }

        public SwiftClient(ISwiftAuthManager authManager, ISwiftLogger logger) : this(authManager)
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

        private T GetExceptionResponse<T>(WebException e, string url) where T : SwiftBaseResponse, new()
        {
            var result = new T();

            var rsp = ((HttpWebResponse)e.Response);

            if (rsp != null)
            {
                result.StatusCode = rsp.StatusCode;
                result.Reason = rsp.StatusDescription;
            }
            else
            {
                result.StatusCode = HttpStatusCode.BadRequest;
                result.Reason = e.Message;
            }

            if (_logger != null)
            {
                _logger.LogRequestError(e, result.StatusCode, result.Reason, url);
            }

            return result;
        }

        private T GetResponse<T>(HttpResponseMessage rsp) where T : SwiftBaseResponse, new()
        {
            var result = new T();
            result.StatusCode = rsp.StatusCode;
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

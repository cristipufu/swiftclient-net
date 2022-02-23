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
        protected bool _customClient;

        public Client() { }

        public Client(SwiftCredentials credentials) : this(new SwiftAuthManager(credentials)) { }

        public Client(SwiftCredentials credentials, ISwiftLogger logger) : this(credentials)
        {
            SetLogger(logger);
        }

        public Client(ISwiftAuthManager authManager)
        {
            if (authManager.Authenticate == null)
            {
                authManager.Authenticate = Authenticate;
            }

            RetryManager = new SwiftRetryManager(authManager);
        }

        public Client(ISwiftAuthManager authManager, ISwiftLogger logger) : this(authManager)
        {
            SetLogger(logger);
        }

        public void SetHttpClient(IHttpClientFactory clientFactory, string httpClientName = "swift", bool noDispose = true)
        {
            _client = clientFactory.CreateClient(httpClientName);
            if (noDispose)
            {
                _customClient = true;
            }
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

            if (ex is WebException webException)
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

        private T GetResponse<T>(HttpResponseMessage rsp) where T : SwiftBaseResponse, new() =>
            new T
            {
                StatusCode = rsp.StatusCode,
                Headers = rsp.Headers.ToDictionary(),
                Reason = rsp.ReasonPhrase,
                ContentLength = rsp.Content.Headers.ContentLength ?? 0
            };

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

            if (disposing && !_customClient)
            {
                _client.Dispose();
            }

            disposed = true;
        }
    }
}

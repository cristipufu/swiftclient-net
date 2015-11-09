using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

using SwiftClient.Extensions;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public abstract partial class SwiftClientBase : ISwiftClient, IDisposable
    {
        protected ISwiftLogger _logger;
        protected SwiftCredentials _credentials;
        protected SwiftRetryManager _manager;
        protected HttpClient _client = new HttpClient();

        public SwiftClientBase()
        {
            _manager = new SwiftRetryManager(
                GetCredentials,
                Authenticate,
                SetAuthData,
                GetAuthData,
                SetEndpoints,
                GetEndpoints);
        }

        public SwiftClientBase(SwiftCredentials credentials) : this()
        {
            _credentials = credentials;
        }

        public SwiftClientBase(SwiftCredentials credentials, SwiftConfig config) : this(credentials)
        {
            if (config != null)
            {
                if (config.RetryCount.HasValue)
                {
                    _manager.SetRetryCount(config.RetryCount.Value);
                }

                if (config.RetryCountPerEndpoint.HasValue)
                {
                    _manager.SetRetryPerEndpointCount(config.RetryCountPerEndpoint.Value);
                }
            }
        }

        public SwiftClientBase(SwiftCredentials credentials, ISwiftLogger logger) : this(credentials)
        {
            _logger = logger;
            _manager.SetLogger(logger);
        }

        public SwiftClientBase(SwiftCredentials credentials, SwiftConfig config, ISwiftLogger logger) : this(credentials, config)
        {
            _logger = logger;
            _manager.SetLogger(logger);
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

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}

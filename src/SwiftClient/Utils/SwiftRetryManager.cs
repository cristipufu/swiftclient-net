using SwiftClient.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SwiftClient.Utils
{
    /// <summary>
    /// Handles authentication, token expiration reauthorization, retry logic with multiple proxy endpoints
    /// </summary>
    public class SwiftRetryManager
    {
        private Func<SwiftCredentials> _credentials;
        private Func<string, string, string, Task<SwiftAuthData>> _authenticate;
        private Action<SwiftAuthData> _setAuthData;
        private Func<SwiftAuthData> _getAuthData;
        private Action<List<string>> _setEndpoints;
        private Func<List<string>> _getEndpoints;

        private ISwiftLogger _logger;

        protected int _retryPerEndpointCount = 1;
        protected int _retryCount = 1;

        public SwiftRetryManager(Func<SwiftCredentials> credentials,
            Func<string, string, string, Task<SwiftAuthData>> authenticate,
            Action<SwiftAuthData> cacheToken,
            Func<SwiftAuthData> getCachedToken,
            Action<List<string>> cacheEndpoints,
            Func<List<string>> getCachedEndpoints)
        {
            _credentials = credentials;
            _authenticate = authenticate;
            _setAuthData = cacheToken;
            _getAuthData = getCachedToken;
            _setEndpoints = cacheEndpoints;
            _getEndpoints = getCachedEndpoints;
        }

        public async Task<SwiftAuthData> Authenticate()
        {
            var credentials = _credentials();

            var retrier = RetryPolicy<string>.Create()
                .WithSteps(_getEndpoints())
                .WithCount(_retryCount)
                .WithCountPerStep(_retryPerEndpointCount);

            SwiftAuthData data = null;

            var success = await retrier.DoAsync(async (endpoint) =>
            {
                data = await _authenticate(credentials.Username, credentials.Password, endpoint);

                return data != null;
            });

            // cache new endpoints order
            _setEndpoints(retrier.GetSteps());

            return data;
        }

        public async Task<T> AuthorizeAndExecute<T>(Func<SwiftAuthData, Task<T>> func) where T : SwiftBaseResponse, new()
        {
            T resp = new T();

            var retrier = RetryPolicy<string>.Create()
                .WithSteps(_getEndpoints())
                .WithCount(_retryCount)
                .WithCountPerStep(_retryPerEndpointCount);

            var isSuccessful = await retrier.DoAsync(async (endpoint) =>
            {
                var auth = await GetOrSetAuthentication(endpoint);

                if (auth == null)
                {
                    // dead proxy node maybe? try with next
                    return false;
                }

                resp = await func(auth);

                // authenticate again on same proxy node
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (_logger != null)
                    {
                        _logger.LogUnauthorizedError(auth.AuthToken, endpoint);
                    }

                    auth = await SetAuthentication(endpoint);

                    resp = await func(auth);
                }

                // try next proxy node
                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    return false;
                }

                if (IsSuccessStatusCode(resp.StatusCode))
                {
                    return true;
                }

                // unknown status code => try next proxy node
                return false;
            });

            resp.IsSuccess = isSuccessful;

            // cache new endpoints order
            _setEndpoints(retrier.GetSteps());

            return resp;
        }

        private async Task<SwiftAuthData> GetOrSetAuthentication(string endpoint)
        {
            var cached = _getAuthData();

            if (cached == null)
            {
                return await SetAuthentication(endpoint);
            }

            return cached;
        }

        private async Task<SwiftAuthData> SetAuthentication(string endpoint)
        {
            var credentials = _credentials();

            if (credentials != null)
            {
                var auth = await _authenticate(credentials.Username, credentials.Password, endpoint);

                if (auth != null)
                {
                    _setAuthData(auth);
                }

                return auth;
            }

            return null;
        }

        private bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.OK ||
                statusCode == HttpStatusCode.Created ||
                statusCode == HttpStatusCode.Accepted ||
                statusCode == HttpStatusCode.NoContent ||
                statusCode == HttpStatusCode.PartialContent;
        }

        public void SetRetryCount(int retryCount)
        {
            _retryCount = retryCount;
        }

        public void SetRetryPerEndpointCount(int retryPerEndpointCount)
        {
            _retryPerEndpointCount = retryPerEndpointCount;
        }

        public void SetLogger(ISwiftLogger logger)
        {
            _logger = logger;
        }
    }
}

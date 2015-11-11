using System;
using System.Net;
using System.Threading.Tasks;

namespace SwiftClient
{
    /// <summary>
    /// Handles authentication, token expiration reauthorization, retry logic with multiple proxy endpoints
    /// </summary>
    public class SwiftRetryManager
    {
        private ISwiftAuthManager _authManager;
        private ISwiftLogger _logger;

        protected int _retryPerEndpointCount = 1;
        protected int _retryCount = 1;

        public SwiftRetryManager(ISwiftAuthManager authManager)
        {
            _authManager = authManager;
        }

        public async Task<SwiftAuthData> Authenticate()
        {
            var retrier = RetryPolicy<string>.Create()
                .WithSteps(_authManager.GetEndpoints())
                .WithCount(_retryCount)
                .WithCountPerStep(_retryPerEndpointCount);

            SwiftAuthData data = null;

            var credentials = _authManager.Credentials;

            var success = await retrier.DoAsync(async (endpoint) =>
            {
                data = await _authManager.Authenticate(credentials.Username, credentials.Password, endpoint);

                return data != null;
            });

            // cache new endpoints order
            _authManager.SetEndpoints(retrier.GetSteps());

            return data;
        }

        public async Task<T> AuthorizeAndExecute<T>(Func<SwiftAuthData, Task<T>> func) where T : SwiftBaseResponse, new()
        {
            T resp = new T();

            var retrier = RetryPolicy<string>.Create()
                .WithSteps(_authManager.GetEndpoints())
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
            _authManager.SetEndpoints(retrier.GetSteps());

            return resp;
        }

        private async Task<SwiftAuthData> GetOrSetAuthentication(string endpoint)
        {
            var cached = _authManager.GetAuthData();

            if (cached == null)
            {
                return await SetAuthentication(endpoint);
            }

            return cached;
        }

        private async Task<SwiftAuthData> SetAuthentication(string endpoint)
        {
            var credentials = _authManager.Credentials;

            if (credentials != null)
            {
                var auth = await _authManager.Authenticate(credentials.Username, credentials.Password, endpoint);

                if (auth != null)
                {
                    _authManager.SetAuthData(auth);
                }

                return auth;
            }

            return null;
        }

        private bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 299);
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

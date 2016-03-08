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
        public ISwiftAuthManager AuthManager;

        private ISwiftLogger _logger;

        protected int _retryPerEndpointCount = 1;
        protected int _retryCount = 1;

        public SwiftRetryManager(ISwiftAuthManager authManager)
        {
            AuthManager = authManager;
        }

        public async Task<SwiftAuthData> Authenticate()
        {
            var retrier = RetryPolicy<string>.Create()
                .WithSteps(AuthManager.GetEndpoints())
                .WithCount(_retryCount)
                .WithCountPerStep(_retryPerEndpointCount);

            SwiftAuthData data = null;

            var credentials = AuthManager.Credentials;

            var success = await retrier.DoAsync(async (endpoint) =>
            {
                data = await AuthManager.Authenticate(credentials.Username, credentials.Password, endpoint).ConfigureAwait(false);

                return data != null;
            }).ConfigureAwait(false);

            // cache new endpoints order
            AuthManager.SetEndpoints(retrier.GetSteps());

            return data;
        }

        public async Task<T> AuthorizeAndExecute<T>(Func<SwiftAuthData, Task<T>> func) where T : SwiftBaseResponse, new()
        {
            T resp = new T();

            var retrier = RetryPolicy<string>.Create()
                .WithSteps(AuthManager.GetEndpoints())
                .WithCount(_retryCount)
                .WithCountPerStep(_retryPerEndpointCount);

            var isSuccessful = await retrier.DoAsync(async (endpoint) =>
            {
                var auth = await GetOrSetAuthentication(endpoint).ConfigureAwait(false);

                if (auth == null)
                {
                    // dead proxy node maybe? try with next
                    return false;
                }

                resp = await func(auth).ConfigureAwait(false);

                // authenticate again on same proxy node
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (_logger != null)
                    {
                        _logger.LogUnauthorizedError(auth.AuthToken, endpoint);
                    }

                    auth = await SetAuthentication(endpoint).ConfigureAwait(false);

                    resp = await func(auth).ConfigureAwait(false);
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
            }).ConfigureAwait(false);

            resp.IsSuccess = isSuccessful;

            // cache new endpoints order
            AuthManager.SetEndpoints(retrier.GetSteps());

            return resp;
        }

        private async Task<SwiftAuthData> GetOrSetAuthentication(string endpoint)
        {
            var cached = AuthManager.GetAuthData();

            if (cached == null)
            {
                return await SetAuthentication(endpoint).ConfigureAwait(false);
            }

            return cached;
        }

        private async Task<SwiftAuthData> SetAuthentication(string endpoint)
        {
            var credentials = AuthManager.Credentials;

            if (credentials != null)
            {
                var auth = await AuthManager.Authenticate(credentials.Username, credentials.Password, endpoint).ConfigureAwait(false);

                if (auth != null)
                {
                    AuthManager.SetAuthData(auth);
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

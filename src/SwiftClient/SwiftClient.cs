using System.Collections.Generic;
using SwiftClient.Models;

namespace SwiftClient
{
    public class SwiftClient : SwiftClientBase
    {
        #region Ctor and Properties

        protected SwiftAuthData _authData;
        protected List<string> _endpoints;

        public SwiftClient() : base() { }

        public SwiftClient(SwiftCredentials credentials) : base(credentials) { }

        public SwiftClient(SwiftCredentials credentials, SwiftConfig config) : base(credentials, config) { }

        public SwiftClient(SwiftCredentials credentials, ISwiftLogger logger) : base(credentials, logger) { }

        public SwiftClient(SwiftCredentials credentials, SwiftConfig config, ISwiftLogger logger) : base(credentials, config, logger) { }

        #endregion

        /// <summary>
        /// Use for caching the authentication token
        /// If you don't cache the authentication token, each swift call will be preceded by an auth call 
        ///     to obtain the token
        /// </summary>
        /// <param name="authData"></param>
        protected override void SetAuthData(SwiftAuthData authData)
        {
            _authData = authData;
        }

        /// <summary>
        /// Get authentication token from cache
        /// </summary>
        /// <returns></returns>
        protected override SwiftAuthData GetAuthData()
        {
            return _authData;
        }

        /// <summary>
        /// Get cached proxy endpoints (ordered by priority)
        /// If you don't cache the list, each swift call will try the proxy nodes in the initial priority order
        /// </summary>
        /// <returns></returns>
        protected override List<string> GetEndpoints()
        {
            return _endpoints ?? _credentials.Endpoints;
        }

        /// <summary>
        /// Save new endpoints order in cache
        /// </summary>
        /// <param name="endpoints"></param>
        protected override void SetEndpoints(List<string> endpoints)
        {
            _endpoints = endpoints;
        }
    }
}

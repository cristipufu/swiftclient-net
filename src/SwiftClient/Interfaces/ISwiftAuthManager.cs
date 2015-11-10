using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwiftClient
{
    public interface ISwiftAuthManager
    {
        SwiftCredentials Credentials { get; set; }
        Func<string, string, string, Task<SwiftAuthData>> Authenticate { get;  set; }

        /// <summary>
        /// Use for caching the authentication token
        /// If you don't cache the authentication token, each swift call will be preceded by an auth call 
        ///     to obtain the token
        /// </summary>
        /// <param name="authData"></param>
        void SetAuthData(SwiftAuthData authData);

        /// <summary>
        /// Get authentication token from cache
        /// </summary>
        /// <returns></returns>
        SwiftAuthData GetAuthData();

        /// <summary>
        /// Get cached proxy endpoints (ordered by priority)
        /// If you don't cache the list, each swift call will try the proxy nodes in the initial priority order
        /// </summary>
        /// <returns></returns>
        List<string> GetEndpoints();

        /// <summary>
        /// Save new endpoints order in cache
        /// </summary>
        /// <param name="endpoints"></param>
        void SetEndpoints(List<string> endpoints);
    }
}

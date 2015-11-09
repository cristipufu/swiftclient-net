using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

using SwiftClient.Extensions;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public abstract partial class SwiftClientBase : ISwiftClient
    {
        /// <summary>
        /// Use for caching the authentication token
        /// If you don't cache the authentication token, each swift call will be preceded by an auth call 
        ///     to obtain the token
        /// </summary>
        /// <param name="authData"></param>
        protected abstract void SetAuthData(SwiftAuthData authData);

        /// <summary>
        /// Get authentication token from cache
        /// </summary>
        /// <returns></returns>
        protected abstract SwiftAuthData GetAuthData();

        /// <summary>
        /// Get cached proxy endpoints (ordered by priority)
        /// If you don't cache the list, each swift call will try the proxy nodes in the initial priority order
        /// </summary>
        /// <returns></returns>
        protected abstract List<string> GetEndpoints();

        /// <summary>
        /// Save new endpoints order in cache
        /// </summary>
        /// <param name="endpoints"></param>
        protected abstract void SetEndpoints(List<string> endpoints);
    }
}

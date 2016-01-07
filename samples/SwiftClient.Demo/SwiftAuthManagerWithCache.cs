using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace SwiftClient.Demo
{
    public class SwiftAuthManagerWithCache : ISwiftAuthManager
    {
        IMemoryCache cache;
        string authCacheKey = "swift_authdata";
        string endpointsKey = "swift_endpoints";

        public SwiftAuthManagerWithCache(SwiftCredentials credentials, IMemoryCache cache)
        {
            Credentials = credentials;
            this.cache = cache;
        }

        public Func<string, string, string, Task<SwiftAuthData>> Authenticate { get; set; }

        public SwiftCredentials Credentials { get; set; }

        public SwiftAuthData GetAuthData()
        {
            return cache.Get<SwiftAuthData>(authCacheKey);
        }

        public void SetAuthData(SwiftAuthData authData)
        {
            if (authData != null)
            {
                cache.Set(authCacheKey, authData);
            }
        }

        public List<string> GetEndpoints()
        {
            return cache.Get<List<string>>(endpointsKey) ?? Credentials.Endpoints;
        }

        public void SetEndpoints(List<string> endpoints)
        {
            if (endpoints != null && endpoints.Any())
            {
                cache.Set(endpointsKey, endpoints);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SwiftClient.AspNetCore
{
    public class SwiftAuthManagerMemoryCache : ISwiftAuthManager
    {
        private readonly IMemoryCache cache;
        private readonly string authCacheKey = "swift_authdata";
        private readonly string endpointsKey = "swift_endpoints";

        public SwiftAuthManagerMemoryCache(IOptions<SwiftServiceOptions> options, IMemoryCache cache)
        {
            var _options = options.Value;
            Credentials = new SwiftCredentials {
                Endpoints = _options.Endpoints,
                Password = _options.Password,
                Username = _options.Username
            };
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

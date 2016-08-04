using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace SwiftClient.AspNetCore
{
    public class SwiftAuthManagerDistributedCache : ISwiftAuthManager
    {
        private readonly IDistributedCache _cache;
        private readonly string authCacheKey = "swift_authdata";
        private readonly string endpointsKey = "swift_endpoints";

        public SwiftAuthManagerDistributedCache(IOptions<SwiftServiceOptions> options, IDistributedCache cache)
        {
            var _options = options.Value;
            Credentials = new SwiftCredentials {
                Endpoints = _options.Endpoints,
                Password = _options.Password,
                Username = _options.Username
            };

            _cache = cache;
        }

        public Func<string, string, string, Task<SwiftAuthData>> Authenticate { get; set; }

        public SwiftCredentials Credentials { get; set; }

        public SwiftAuthData GetAuthData()
        {
            var stored = _cache.GetString(authCacheKey);

            if (!string.IsNullOrEmpty(stored))
            {
                return JsonConvert.DeserializeObject<SwiftAuthData>(stored);
            }

            return null;
        }

        public void SetAuthData(SwiftAuthData authData)
        {
            if (authData != null)
            {
                _cache.SetString(authCacheKey, JsonConvert.SerializeObject(authData));
            }
        }

        public List<string> GetEndpoints()
        {
            var stored = _cache.GetString(endpointsKey);

            if (!string.IsNullOrEmpty(stored))
            {
                return JsonConvert.DeserializeObject<List<string>>(stored);
            }

            return Credentials.Endpoints;
        }

        public void SetEndpoints(List<string> endpoints)
        {
            if (endpoints != null && endpoints.Any())
            {
                _cache.SetString(endpointsKey, JsonConvert.SerializeObject(endpoints));
            }
        }
    }
}

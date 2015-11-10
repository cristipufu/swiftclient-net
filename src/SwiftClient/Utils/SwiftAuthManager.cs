using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwiftClient
{
    public class SwiftAuthManager : ISwiftAuthManager
    {
        SwiftAuthData _authData;
        List<string> _endpoints;

        public Func<string, string, string, Task<SwiftAuthData>> Authenticate { get; set; }

        public SwiftCredentials Credentials { get; set; }

        public SwiftAuthManager() { }

        public SwiftAuthManager(SwiftCredentials credentials)
        {
            Credentials = credentials;
        }

        public SwiftAuthData GetAuthData()
        {
            return _authData;
        }

        public List<string> GetEndpoints()
        {
            return _endpoints ?? Credentials.Endpoints;
        }

        public void SetAuthData(SwiftAuthData authData)
        {
            _authData = authData;
        }

        public void SetEndpoints(List<string> endpoints)
        {
            _endpoints = endpoints;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SwiftClient.Extensions;

namespace SwiftClient
{
    public partial class Client : ISwiftClient, IDisposable
    {
        /// <summary>
        /// Get authentication token and storage url
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task<SwiftAuthData> Authenticate()
        {
            return _manager.Authenticate();
        }

        private async Task<SwiftAuthData> Authenticate(string username, string password, string endpoint)
        {
            var url = SwiftUrlBuilder.GetAuthUrl(endpoint);

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            FillRequest(request, null, new Dictionary<string, string>
            {
                { SwiftHeaderKeys.AuthUser, username },
                { SwiftHeaderKeys.AuthKey, password }
            });

            try
            {
                using (var response = await _client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    return new SwiftAuthData
                    {
                        AuthToken = response.GetHeader(SwiftHeaderKeys.AuthToken),
                        StorageUrl = response.GetHeader(SwiftHeaderKeys.StorageUrl)
                    };
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogAuthenticationError(ex, username, password, endpoint);
                }

                return null;
            }
        }

        private Task<T> AuthorizeAndExecute<T>(Func<SwiftAuthData, Task<T>> func) where T : SwiftBaseResponse, new()
        {
            return _manager.AuthorizeAndExecute(func);
        }
    }
}

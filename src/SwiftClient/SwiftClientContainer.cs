using System;
using System.Collections.Generic;
using System.Net;

using SwiftClient.Extensions;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public partial class SwiftClient : ISwiftClient, IDisposable
    {

        public Task<SwiftContainerResponse> HeadContainer(string containerId, Dictionary<string, string> headers = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetContainerUrl(auth.StorageUrl, containerId);

                var request = new HttpRequestMessage(HttpMethod.Head, url);

                FillRequest(request, auth);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        var result = GetResponse<SwiftContainerResponse>(response);

                        long totalBytes, objectsCount;

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.ContainerBytesUsed), out totalBytes))
                        {
                            result.TotalBytes = totalBytes;
                        }

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.ContainerObjectCount), out objectsCount))
                        {
                            result.ObjectsCount = objectsCount;
                        }

                        return result;
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftContainerResponse>(e, url);
                }
            });
        }

        public Task<SwiftContainerResponse> GetContainer(string containerId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetContainerUrl(auth.StorageUrl, containerId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                FillRequest(request, auth, headers);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        var result = GetResponse<SwiftContainerResponse>(response);

                        long totalBytes, objectsCount;

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.ContainerBytesUsed), out totalBytes))
                        {
                            result.TotalBytes = totalBytes;
                        }

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.ContainerObjectCount), out objectsCount))
                        {
                            result.ObjectsCount = objectsCount;
                        }

                        result.Info = await response.Content.ReadAsStringAsync();

                        return result;
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftContainerResponse>(e, url);
                }
            });
        }

        public Task<SwiftResponse> PutContainer(string containerId, Dictionary<string, string> headers = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetContainerUrl(auth.StorageUrl, containerId);

                var request = new HttpRequestMessage(HttpMethod.Put, url);

                FillRequest(request, auth, headers);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        return GetResponse<SwiftResponse>(response);
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftResponse>(e, url);
                }
            });
        }

        public Task<SwiftResponse> PostContainer(string containerId, Dictionary<string, string> headers = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetContainerUrl(auth.StorageUrl, containerId);

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                FillRequest(request, auth, headers);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        return GetResponse<SwiftResponse>(response);
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftResponse>(e, url);
                }
            });
        }

        public Task<SwiftResponse> DeleteContainer(string containerId, Dictionary<string, string> headers = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetContainerUrl(auth.StorageUrl, containerId);

                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                FillRequest(request, auth, headers);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        return GetResponse<SwiftResponse>(response);
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftResponse>(e, url);
                }
            });
        }

    }
}

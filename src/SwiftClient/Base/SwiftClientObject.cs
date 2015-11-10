using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public partial class SwiftClient : ISwiftClient, IDisposable
    {
        public Task<SwiftResponse> HeadObject(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Head, url);

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

        public Task<SwiftResponse> GetObject(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                FillRequest(request, auth, headers);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        var result = GetResponse<SwiftResponse>(response);

                        result.Stream = new MemoryStream();
                        await response.Content.CopyToAsync(result.Stream);
                        result.Stream.Position = 0;

                        return result;
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftResponse>(e, url);
                }
            });
        }

        public Task<SwiftResponse> GetObjectRange(string containerId, string objectId, long start, long end, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            headers["Range"] = string.Format("bytes={0}-{1}", start, end);

            return GetObject(containerId, objectId, headers, queryParams);
        }

        public Task<SwiftResponse> PostObject(string containerId, string objectId, Dictionary<string, string> headers = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId);

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

        public Task<SwiftResponse> PutObject(string containerId, string objectId, Stream data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Put, url);

                FillRequest(request, auth, headers);

                try
                {
                    request.Content = new StreamContent(data);

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

        public Task<SwiftResponse> PutObject(string containerId, string objectId, byte[] data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Put, url);

                FillRequest(request, auth, headers);

                try
                {
                    request.Content = new ByteArrayContent(data);

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

        public Task<SwiftResponse> PutChunkedObject(string containerId, string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, string.Format("{0}.seg{1}", objectId, segment.ToString("00000")), queryParams);

                var request = new HttpRequestMessage(HttpMethod.Put, url);

                FillRequest(request, auth, headers);

                try
                {
                    request.Content = new ByteArrayContent(data);

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

        public Task<SwiftResponse> PutManifest(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                if (headers == null)
                {
                    headers = new Dictionary<string, string>();
                }

                headers[SwiftHeaderKeys.ObjectManifest] = string.Format("{0}/{1}.seg", containerId, objectId);
                headers["Content-Length"] = "0";

                var request = new HttpRequestMessage(HttpMethod.Put, url);

                FillRequest(request, auth, headers);

                try
                {
                    request.Content = new ByteArrayContent(new byte[0]);

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

        public Task<SwiftResponse> CopyObject(string containerFromId, string objectFromId, string containerToId, string objectToId, Dictionary<string, string> headers = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            headers[SwiftHeaderKeys.CopyFrom] = containerFromId + "/" + objectFromId;

            return PutObject(containerToId, objectToId, new byte[0], headers);
        }

        public Task<SwiftResponse> DeleteObject(string containerId, string objectId)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var queryParams = new Dictionary<string, string>() { { "multipart-manifest", "delete" } };

                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                FillRequest(request, auth);

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

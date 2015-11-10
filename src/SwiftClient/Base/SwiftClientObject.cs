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

            headers[SwiftHeaderKeys.Range] = string.Format(SwiftHeaderKeys.RangeValueFormat, start, end);

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
            return PutObject(containerId, SwiftUrlBuilder.GetObjectChunkId(objectId, segment), data, headers, queryParams);
        }

        public Task<SwiftResponse> PutManifest(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            int contentLength = 0;

            headers[SwiftHeaderKeys.ObjectManifest] = string.Format(SwiftHeaderKeys.ObjectManifestValueFormat, containerId, objectId);
            headers[SwiftHeaderKeys.ContentLength] = contentLength.ToString();

            return PutObject(containerId, objectId, new byte[contentLength], headers, queryParams);
        }

        public Task<SwiftResponse> CopyObject(string containerFromId, string objectFromId, string containerToId, string objectToId, Dictionary<string, string> headers = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            headers[SwiftHeaderKeys.CopyFrom] = string.Format(SwiftHeaderKeys.ObjectManifestValueFormat, containerFromId, objectFromId);

            return PutObject(containerToId, objectToId, new byte[0], headers);
        }

        public Task<SwiftResponse> DeleteObject(string containerId, string objectId)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                // unfortunately no api support for DLO delete
                // so deleting the manifest file won't delete the object segments
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

        public Task<SwiftResponse> DeleteObjectChunk(string containerId, string objectId, int segment)
        {
            return DeleteObject(containerId, SwiftUrlBuilder.GetObjectChunkId(objectId, segment));
        }
    }
}

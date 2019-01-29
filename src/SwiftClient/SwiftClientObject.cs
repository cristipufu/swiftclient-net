using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Net;
using SwiftClient.Extensions;

namespace SwiftClient
{
    public partial class Client : ISwiftClient, IDisposable
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
                    using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        return GetResponse<SwiftResponse>(response);
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
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
                    var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                    var result = GetResponse<SwiftResponse>(response);

                    if (response.IsSuccessStatusCode)
                    {
                        result.Stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
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
                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        return GetResponse<SwiftResponse>(response);
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
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

                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        var result = GetResponse<SwiftResponse>(response);

                        // container not found
                        if (result.StatusCode == HttpStatusCode.NotFound)
                        {
                            return await EnsurePutContainer(containerId, () => PutObject(containerId, objectId, data, headers, queryParams)).ConfigureAwait(false);
                        }

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
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

                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        var result = GetResponse<SwiftResponse>(response);

                        // container not found
                        if (result.StatusCode == HttpStatusCode.NotFound)
                        {
                            return await EnsurePutContainer(containerId, () => PutObject(containerId, objectId, data, headers, queryParams)).ConfigureAwait(false);
                        }

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
                }
            });
        }

        private async Task<SwiftResponse> EnsurePutContainer(string containerId, Func<Task<SwiftResponse>> retryFunc)
        {
            // put container
            var putContainerRsp = await PutContainer(containerId).ConfigureAwait(false);

            if (!putContainerRsp.IsSuccess)
            {
                return putContainerRsp;
            }

            // retry put object
            return await retryFunc().ConfigureAwait(false);
        }

        public Task<SwiftResponse> PutObjectChunk(string containerId, string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
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

            headers[SwiftHeaderKeys.ObjectManifest] = SwiftHeaderKeys.GetObjectManifestValue(containerId, objectId);
            headers[SwiftHeaderKeys.ContentLength] = contentLength.ToString();

            return PutObject(containerId, objectId, new byte[contentLength], headers, queryParams);
        }

        public Task<SwiftResponse> PutPseudoDirectory(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Put, url);

                FillRequest(request, auth, headers);

                try
                {
                    var data = new byte[0];

                    request.Content = new ByteArrayContent(data);
                    request.Content.SetHeaders(new Dictionary<string, string> { { SwiftHeaderKeys.ContentType, "application/directory" } });

                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        var result = GetResponse<SwiftResponse>(response);

                        // container not found
                        if (result.StatusCode == HttpStatusCode.NotFound)
                        {
                            return await EnsurePutContainer(containerId, () => PutObject(containerId, objectId, data, headers, queryParams)).ConfigureAwait(false);
                        }

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
                }
            });
        }

        public Task<SwiftResponse> CopyObject(string containerFromId, string objectFromId, string containerToId, string objectToId, Dictionary<string, string> headers = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            headers[SwiftHeaderKeys.CopyFrom] = SwiftHeaderKeys.GetObjectManifestValue(containerFromId, objectFromId);

            return PutObject(containerToId, objectToId, new byte[0], headers);
        }

        /// <summary>
        /// Delete object. 
        /// If ([filter:slo]) is configured and you want to delete SLO file including segments add {"multipart-manifest", "delete"} to queryParams
        /// </summary>
        /// <param name="containerId"></param>
        /// <param name="objectId"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        public Task<SwiftResponse> DeleteObject(string containerId, string objectId, Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetObjectUrl(auth.StorageUrl, containerId, objectId, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                FillRequest(request, auth);

                try
                {
                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        return GetResponse<SwiftResponse>(response);
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
                }
            });
        }

        /// <summary>
        /// Delete object chunk.
        /// Unfortunately no api support for DLO delete ([filter:dlo]). 
        /// Deleting the manifest file won't delete the object segments.
        /// </summary>
        /// <param name="containerId"></param>
        /// <param name="objectId"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public Task<SwiftResponse> DeleteObjectChunk(string containerId, string objectId, int segment)
        {
            return DeleteObject(containerId, SwiftUrlBuilder.GetObjectChunkId(objectId, segment));
        }

        /// <summary>
        /// Bulk delete objects in a specified container (option available for [filter:bulk] in proxy-server.conf)
        /// </summary>
        /// <param name="containerId"></param>
        /// <param name="objectIds"></param>
        /// <returns></returns>
        public Task<SwiftResponse> DeleteObjects(string containerId, IEnumerable<string> objectIds)
        {
            return DeleteObjects(objectIds.Select(x => containerId + "/" + x).ToList());
        }

        /// <summary>
        /// Bulk delete objects (option available for [filter:bulk] in proxy-server.conf)
        /// Object id can be <container_id>, <container_id>/<object_id>
        /// Example input: 
        /// alpha/one.txt
        /// alpha/two.txt
        /// alpha
        /// beta/three.txt
        /// beta/four.txt
        /// beta
        /// </summary>
        /// <param name="objectIds"></param>
        /// <returns>Json formatted string with info about the delete operation</returns>
        public Task<SwiftResponse> DeleteObjects(IEnumerable<string> objectIds)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = auth.StorageUrl + "?bulk-delete=true";

                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                request.Headers.Add("Accept", "application/json");

                FillRequest(request, auth);

                var data = string.Join(Environment.NewLine, objectIds);

                request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(data));

                try
                {
                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        var result = GetResponse<SwiftResponse>(response);

                        if (response.IsSuccessStatusCode)
                        {
                            result.Stream = new MemoryStream();
                            await response.Content.CopyToAsync(result.Stream).ConfigureAwait(false);
                            result.Stream.Position = 0;
                        }

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    return GetExceptionResponse<SwiftResponse>(ex, url);
                }
            });
        }
    }
}

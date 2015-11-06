using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

using SwiftClient.Extensions;
using SwiftClient.Models;
using SwiftClient.Utils;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public abstract class SwiftClientBase : ISwiftClient
    {
        #region Ctor and Properties

        protected ISwiftLogger _logger;
        protected SwiftCredentials _credentials;
        protected SwiftRetryManager _manager;
        protected HttpClient _client = new HttpClient();

        public SwiftClientBase()
        {
            _manager = new SwiftRetryManager(
                GetCredentials,
                Authenticate,
                SetAuthData,
                GetAuthData,
                SetEndpoints,
                GetEndpoints);
        }

        public SwiftClientBase(SwiftCredentials credentials) : this()
        {
            _credentials = credentials;
        }

        public SwiftClientBase(SwiftCredentials credentials, SwiftConfig config) : this(credentials)
        {
            if (config != null)
            {
                if (config.RetryCount.HasValue)
                {
                    _manager.SetRetryCount(config.RetryCount.Value);
                }

                if (config.RetryCountPerEndpoint.HasValue)
                {
                    _manager.SetRetryPerEndpointCount(config.RetryCountPerEndpoint.Value);
                }
            }
        }

        public SwiftClientBase(SwiftCredentials credentials, ISwiftLogger logger) : this(credentials)
        {
            _logger = logger;
            _manager.SetLogger(logger);
        }

        public SwiftClientBase(SwiftCredentials credentials, SwiftConfig config, ISwiftLogger logger) : this(credentials, config)
        {
            _logger = logger;
            _manager.SetLogger(logger);
        }

        #endregion

        #region Config

        /// <summary>
        /// Set credentials (username, password, list of proxy endpoints)
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public SwiftClientBase WithCredentials(SwiftCredentials credentials)
        {
            _credentials = credentials;

            return this;
        }

        /// <summary>
        /// Log authentication errors, reauthorization events and request errors
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public SwiftClientBase SetLogger(ISwiftLogger logger)
        {
            _logger = logger;

            return this;
        }

        /// <summary>
        /// Set retries count for all proxy nodes
        /// </summary>
        /// <param name="retryCount">Default value 1</param>
        /// <returns></returns>
        public SwiftClientBase SetRetryCount(int retryCount)
        {
            _manager.SetRetryCount(retryCount);

            return this;
        }

        /// <summary>
        /// Set retries count per proxy node request
        /// </summary>
        /// <param name="retryPerEndpointCount">Default value 1</param>
        /// <returns></returns>
        public SwiftClientBase SetRetryPerEndpointCount(int retryPerEndpointCount)
        {
            _manager.SetRetryPerEndpointCount(retryPerEndpointCount);

            return this;
        }

        #endregion

        #region Cache override implementation

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

        #endregion

        #region Authorization

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

        private SwiftCredentials GetCredentials()
        {
            return _credentials;
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
                    return new SwiftAuthData
                    {
                        AuthToken = response.GetHeader(SwiftHeaderKeys.AuthToken),
                        StorageUrl = response.GetHeader(SwiftHeaderKeys.StorageUrl)
                    };
                }
            }
            catch (WebException e)
            {
                if (_logger != null)
                {
                    _logger.LogAuthenticationError(e, username, password, endpoint);
                }

                return null;
            }
        }

        private Task<T> AuthorizeAndExecute<T>(Func<SwiftAuthData, Task<T>> func) where T : SwiftBaseResponse, new()
        {
            return _manager.AuthorizeAndExecute(func);
        }

        #endregion

        #region Account

        public Task<SwiftAccountResponse> HeadAccount()
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetAccountUrl(auth.StorageUrl);

                var request = new HttpRequestMessage(HttpMethod.Head, url);

                FillRequest(request, auth);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        var result = GetResponse<SwiftAccountResponse>(response);

                        long totalBytes, containersCount, objectsCount;

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.AccountBytesUsed), out totalBytes))
                        {
                            result.TotalBytes = totalBytes;
                        }

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.AccountContainerCount), out containersCount))
                        {
                            result.ContainersCount = containersCount;
                        }

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.AccountObjectCount), out objectsCount))
                        {
                            result.ObjectsCount = objectsCount;
                        }

                        return result;
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftAccountResponse>(e, auth.StorageUrl);
                }
            });
        }

        public Task<SwiftAccountResponse> GetAccount(Dictionary<string, string> queryParams = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetAccountUrl(auth.StorageUrl, queryParams);

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                FillRequest(request, auth);

                try
                {
                    using (var response = await _client.SendAsync(request))
                    {
                        var result = GetResponse<SwiftAccountResponse>(response);

                        long totalBytes, containersCount, objectsCount;

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.AccountBytesUsed), out totalBytes))
                        {
                            result.TotalBytes = totalBytes;
                        }

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.AccountContainerCount), out containersCount))
                        {
                            result.ContainersCount = containersCount;
                        }

                        if (long.TryParse(response.GetHeader(SwiftHeaderKeys.AccountObjectCount), out objectsCount))
                        {
                            result.ObjectsCount = objectsCount;
                        }

                        result.Info = await response.Content.ReadAsStringAsync();

                        return result;
                    }
                }
                catch (WebException e)
                {
                    return GetExceptionResponse<SwiftAccountResponse>(e, auth.StorageUrl);
                }
            });
        }

        public Task<SwiftResponse> PostAccount(Dictionary<string, string> headers = null)
        {
            return AuthorizeAndExecute(async (auth) =>
            {
                var url = SwiftUrlBuilder.GetAccountUrl(auth.StorageUrl);

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
                    return GetExceptionResponse<SwiftResponse>(e, auth.StorageUrl);
                }
            });
        }

        #endregion

        #region Container

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

        #endregion

        #region Object

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

        #endregion

        #region Helpers

        private void FillRequest(HttpRequestMessage request, SwiftAuthData auth, Dictionary<string, string> headers = null)
        {
            // set headers
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            if (auth != null)
            {
                headers[SwiftHeaderKeys.AuthToken] = auth.AuthToken;
            }

            request.SetHeaders(headers);
        }

        private T GetExceptionResponse<T>(WebException e, string url) where T : SwiftBaseResponse, new()
        {
            var result = new T();

            var rsp = ((HttpWebResponse)e.Response);

            if (rsp != null)
            {
                result.StatusCode = rsp.StatusCode;
                result.Reason = rsp.StatusDescription;
            }
            else
            {
                result.StatusCode = HttpStatusCode.BadRequest;
                result.Reason = e.Message;
            }

            if (_logger != null)
            {
                _logger.LogRequestError(e, result.StatusCode, result.Reason, url);
            }

            return result;
        }

        private T GetResponse<T>(HttpResponseMessage rsp) where T : SwiftBaseResponse, new()
        {
            var result = new T();
            result.StatusCode = rsp.StatusCode;
            result.Reason = rsp.ReasonPhrase;
            result.ContentLength = rsp.Content.Headers.ContentLength ?? 0;
            return result;
        }

        #endregion
    }
}

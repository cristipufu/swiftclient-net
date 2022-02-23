using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient.AspNetCore
{
    public class SwiftService : Client, ISwiftService
    {
        private readonly SwiftServiceOptions _options;

        public SwiftService(IOptions<SwiftServiceOptions> options,
            ISwiftAuthManager authManager,
            IHttpClientFactory httpClientFactory,
            ISwiftLogger logger,
            string httpClientName = "swift") : base(authManager, logger)
        {
            _options = options.Value;
            SetRetryCount(_options.RetryCount);
            SetHttpClient(httpClientFactory, httpClientName, _options.NoHttpDispose);
            SetRetryPerEndpointCount(_options.RetryPerEndpointCount);
        }

        public Task<SwiftResponse> DeleteDefaultContainerObjectsAsync(IEnumerable<string> objectIds)
            => DeleteObjectsAsync(_options.DefaultContainer, objectIds);

        public Task<SwiftResponse> DeleteObjectAsync(string objectId, Dictionary<string, string> queryParams = null)
            => DeleteObjectAsync(_options.DefaultContainer, objectId, queryParams);

        public Task<SwiftResponse> DeleteObjectChunkAsync(string objectId, int segment)
            => DeleteObjectChunkAsync(_options.DefaultContainer, objectId, segment);

        public Task<SwiftResponse> GetObjectAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => GetObjectAsync(_options.DefaultContainer, objectId, headers, queryParams);

        public Task<SwiftResponse> GetObjectRangeAsync(string objectId, long start, long end, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => GetObjectRangeAsync(_options.DefaultContainer, objectId, start, end, headers, queryParams);

        public Task<SwiftResponse> HeadObjectAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => HeadObjectAsync(_options.DefaultContainer, objectId, headers, queryParams);

        public Task<SwiftResponse> PostObjectAsync(string objectId, Dictionary<string, string> headers = null)
            => PostObjectAsync(_options.DefaultContainer, objectId, headers);

        public Task<SwiftResponse> PutManifestAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => PutManifestAsync(_options.DefaultContainer, objectId, headers, queryParams);

        public Task<SwiftResponse> PutObjectAsync(string objectId, Stream data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => PutObjectAsync(_options.DefaultContainer, objectId, data, headers, queryParams);

        public Task<SwiftResponse> PutObjectAsync(string objectId, byte[] data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => PutObjectAsync(_options.DefaultContainer, objectId, data, headers, queryParams);

        public Task<SwiftResponse> PutObjectChunkAsync(string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => PutObjectChunkAsync(_options.DefaultContainer, objectId, data, segment, headers, queryParams);

        public Task<SwiftResponse> PutPseudoDirectoryAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null)
            => PutPseudoDirectoryAsync(_options.DefaultContainer, objectId, headers, queryParams);
    }
}

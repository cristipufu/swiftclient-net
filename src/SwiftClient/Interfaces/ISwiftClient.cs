using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public interface ISwiftClient
    {       
        void SetHttpClient(IHttpClientFactory httpClientFactory = null, bool noDispose = true);

        Task<SwiftAuthData> Authenticate();
        Task<SwiftResponse> CopyObject(string containerFromId, string objectFromId, string containerToId, string objectToId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> DeleteContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> DeleteObject(string containerId, string objectId, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> DeleteObjectChunk(string containerId, string objectId, int segment);
        Task<SwiftResponse> DeleteObjects(IEnumerable<string> objectIds);
        Task<SwiftResponse> DeleteObjects(string containerId, IEnumerable<string> objectIds);

        Task<SwiftAccountResponse> GetAccount(Dictionary<string, string> queryParams = null);
        Task<SwiftContainerResponse> GetContainer(string containerId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> GetObject(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> GetObjectRange(string containerId, string objectId, long start, long end, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftAccountResponse> HeadAccount();
        Task<SwiftContainerResponse> HeadContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> HeadObject(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PostAccount(Dictionary<string, string> headers = null);
        Task<SwiftResponse> PostContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PostObject(string containerId, string objectId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PutContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PutManifest(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObject(string containerId, string objectId, Stream data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObject(string containerId, string objectId, byte[] data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObjectChunk(string containerId, string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutPseudoDirectory(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        SwiftCredentials GetCredentials();
        Client SetLogger(ISwiftLogger logger);        
        Client SetRetryCount(int retryCount);
        Client SetRetryPerEndpointCount(int retryPerEndpointCount);
        Client WithCredentials(SwiftCredentials credentials);
    }
}
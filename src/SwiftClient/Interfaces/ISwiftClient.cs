using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public interface ISwiftClient
    {       
        void SetHttpClient(IHttpClientFactory httpClientFactory = null, string httpClientName = "", bool noDispose = true);

        Task<SwiftAuthData> AuthenticateAsync();
        Task<SwiftResponse> CopyObjectAsync(string containerFromId, string objectFromId, string containerToId, string objectToId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> DeleteContainerAsync(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> DeleteObjectAsync(string containerId, string objectId, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> DeleteObjectChunkAsync(string containerId, string objectId, int segment);
        Task<SwiftResponse> DeleteObjectsAsync(IEnumerable<string> objectIds);
        Task<SwiftResponse> DeleteObjectsAsync(string containerId, IEnumerable<string> objectIds);

        Task<SwiftAccountResponse> GetAccountAsync(Dictionary<string, string> queryParams = null);
        Task<SwiftContainerResponse> GetContainerAsync(string containerId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> GetObjectAsync(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> GetObjectRangeAsync(string containerId, string objectId, long start, long end, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftAccountResponse> HeadAccountAsync();
        Task<SwiftContainerResponse> HeadContainerAsync(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> HeadObjectAsync(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PostAccountAsync(Dictionary<string, string> headers = null);
        Task<SwiftResponse> PostContainerAsync(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PostObjectAsync(string containerId, string objectId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PutContainerAsync(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PutManifestAsync(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObjectAsync(string containerId, string objectId, Stream data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObjectAsync(string containerId, string objectId, byte[] data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObjectChunkAsync(string containerId, string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutPseudoDirectoryAsync(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        SwiftCredentials GetCredentials();
        Client SetLogger(ISwiftLogger logger);        
        Client SetRetryCount(int retryCount);
        Client SetRetryPerEndpointCount(int retryPerEndpointCount);
        Client WithCredentials(SwiftCredentials credentials);
    }
}
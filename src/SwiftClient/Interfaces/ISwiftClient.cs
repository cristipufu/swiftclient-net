using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SwiftClient
{
    public interface ISwiftClient
    {
        Task<SwiftAuthData> Authenticate();

        Task<SwiftAccountResponse> HeadAccount();
        Task<SwiftAccountResponse> GetAccount(Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PostAccount(Dictionary<string, string> headers = null);

        Task<SwiftContainerResponse> HeadContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftContainerResponse> GetContainer(string containerId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> PostContainer(string containerId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> DeleteContainer(string containerId, Dictionary<string, string> headers = null);

        Task<SwiftResponse> HeadObject(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> GetObject(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> GetObjectRange(string containerId, string objectId, long start, long end, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObject(string containerId, string objectId, byte[] data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObject(string containerId, string objectId, Stream data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutObjectChunk(string containerId, string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> PutManifest(string containerId, string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> CopyObject(string containerFromId, string objectFromId, string containerToId, string objectToId, Dictionary<string, string> headers = null);
        Task<SwiftResponse> DeleteObject(string containerId, string objectId, Dictionary<string, string> queryParams = null);
        Task<SwiftResponse> DeleteObjectChunk(string containerId, string objectId, int segment);
    }
}

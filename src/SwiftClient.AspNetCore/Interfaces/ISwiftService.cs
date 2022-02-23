using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SwiftClient.AspNetCore
{
    public interface ISwiftService: ISwiftClient
    {
        /// <summary> 
        /// Deletes an object in the default container
        /// </summary>
        Task<SwiftResponse> DeleteObjectAsync(string objectId, Dictionary<string, string> queryParams = null);

        /// <summary>
        /// Deletes an object chunk in the default container
        /// </summary>
        Task<SwiftResponse> DeleteObjectChunkAsync(string objectId, int segment);

        /// <summary> 
        /// Deletes an object list in the default container
        /// </summary>
        Task<SwiftResponse> DeleteDefaultContainerObjectsAsync(IEnumerable<string> objectIds);

        /// <summary> 
        /// Gets an object in the default container
        /// </summary>
        Task<SwiftResponse> GetObjectAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
       
        /// <summary>
        /// Gets an object range in the default container
        /// </summary>
        Task<SwiftResponse> GetObjectRangeAsync(string objectId, long start, long end, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);

        /// <summary>
        /// Gets an object (HEAD http method) in the default container
        /// </summary>
        Task<SwiftResponse> HeadObjectAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);

        /// <summary>
        /// Puts an object in the default container
        /// </summary>
        Task<SwiftResponse> PostObjectAsync(string objectId, Dictionary<string, string> headers = null);

        /// <summary>
        /// Puts a manifest in the default container
        /// </summary>
        Task<SwiftResponse> PutManifestAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);

        /// <summary> 
        /// Puts an object in the default container built with its data (stream)
        /// </summary>
        Task<SwiftResponse> PutObjectAsync(string objectId, Stream data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);

        /// <summary> 
        /// Puts an object in the default container built its data (bytes)
        /// </summary>
        Task<SwiftResponse> PutObjectAsync(string objectId, byte[] data, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);

        /// <summary> 
        /// Puts an object chunk in the default container with its data (bytes)
        /// </summary>
        Task<SwiftResponse> PutObjectChunkAsync(string objectId, byte[] data, int segment, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);

        /// <summary> 
        /// Puts a pseduo directory in the default container
        /// </summary>
        Task<SwiftResponse> PutPseudoDirectoryAsync(string objectId, Dictionary<string, string> headers = null, Dictionary<string, string> queryParams = null);
    }
}

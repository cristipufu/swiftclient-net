using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;

namespace SwiftClient
{
    public static class ClientExtensions
    {
        public static async Task<SwiftBaseResponse> PutLargeObject(this ISwiftClient client, string containerId, string objectId, Stream stream, Dictionary<string, string> headers = null, Action<long, long> progress = null, long bufferSize = 1000000, bool checkIntegrity = false)
        {
            SwiftBaseResponse response = null;
            byte[] buffer = new byte[bufferSize];
            string containerTemp = "tmp_" + Guid.NewGuid().ToString("N");
            int bytesRead, chunk = 0;

            response = await client.PutContainerAsync(containerTemp).ConfigureAwait(false);

            if (!response.IsSuccess)
            {
                return response;
            }

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                using (MemoryStream tmpStream = new MemoryStream())
                {
                    tmpStream.Write(buffer, 0, bytesRead);
                    response = await client.PutObjectChunkAsync(containerTemp, objectId, tmpStream.ToArray(), chunk).ConfigureAwait(false);
                }

                progress?.Invoke(chunk, bytesRead);

                if (!response.IsSuccess)
                {
                    // cleanup
                    await client.DeleteContainerWithContents(containerTemp).ConfigureAwait(false);

                    return response;
                }

                chunk++;
            }

            Dictionary<string, string> integrityHeaders = null;

            if (checkIntegrity)
            {
                using (var md5 = MD5.Create())
                {
                    var eTag = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();

                    integrityHeaders = new Dictionary<string, string>() { { "ETag", eTag } };
                }
            }


            // use manifest to merge chunks
            response = await client.PutManifestAsync(containerTemp, objectId, integrityHeaders).ConfigureAwait(false);

            if (!response.IsSuccess)
            {
                // cleanup
                await client.DeleteContainerWithContents(containerTemp).ConfigureAwait(false);

                return response;
            }

            // copy chunks to new file
            response = await client.CopyObjectAsync(containerTemp, objectId, containerId, objectId, headers).ConfigureAwait(false);

            if (!response.IsSuccess)
            {
                // cleanup
                await client.DeleteContainerWithContents(containerTemp).ConfigureAwait(false);

                return response;
            }

            // cleanup temp
            return await client.DeleteContainerWithContents(containerTemp).ConfigureAwait(false);
        }

        public static async Task<SwiftBaseResponse> DeleteContainerWithContents(this ISwiftClient client, string containerId, int limit = 1000)
        {
            // delete all container objects
            var deleteRsp = await client.DeleteContainerContents(containerId, limit).ConfigureAwait(false);

            if (deleteRsp.IsSuccess)
            {
                //delete container
                return await client.DeleteContainerAsync(containerId).ConfigureAwait(false);
            }

            return deleteRsp;
        }

        public static async Task<SwiftBaseResponse> DeleteContainerContents(this ISwiftClient client, string containerId, int limit = 1000)
        {
            var limitHeaderKey = "limit";
            var markerHeaderKey = "marker";

            var queryParams = new Dictionary<string, string>()
            {
                { limitHeaderKey, limit.ToString() }
            };

            var marker = string.Empty;

            while (true)
            {
                if (!string.IsNullOrEmpty(marker))
                {
                    if (queryParams.ContainsKey(markerHeaderKey))
                    {
                        queryParams[markerHeaderKey] = marker;
                    }
                    else
                    {
                        queryParams.Add(markerHeaderKey, marker);
                    }
                }

                // get objects
                var infoRsp = await client.GetContainerAsync(containerId, null, queryParams).ConfigureAwait(false);

                // no more objects => break
                if (infoRsp.ObjectsCount == 0) return infoRsp;

                if (infoRsp.IsSuccess && infoRsp.Objects != null)
                {
                    var objectIds = infoRsp.Objects.Select(x => containerId + "/" + x.Object).ToList();

                    var count = infoRsp.Objects.Count;

                    // delete them
                    var deleteRsp = await client.DeleteObjectsAsync(objectIds).ConfigureAwait(false);

                    if (!deleteRsp.IsSuccess) return deleteRsp;

                    // last page => break
                    if (count < limit) return deleteRsp;

                    marker = infoRsp.Objects.Select(x => x.Object).LastOrDefault();
                }
                else
                {
                    return infoRsp;
                }
            }
        }
    }
}

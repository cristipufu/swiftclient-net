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
        public static async Task<SwiftBaseResponse> PutLargeObject(this Client client, string containerId, string objectId, Stream stream, Dictionary<string, string> headers = null, Action<long, long> progress = null, long bufferSize = 1000000)
        {
            SwiftBaseResponse response = null;
            byte[] buffer = new byte[bufferSize];
            string containerTemp = "tmp_" + Guid.NewGuid().ToString("N");
            int bytesRead, chunk = 0;

            response = await client.PutContainer(containerTemp);

            if (!response.IsSuccess)
            {
                return response;
            }

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                using (MemoryStream tmpStream = new MemoryStream())
                {
                    tmpStream.Write(buffer, 0, bytesRead);
                    response = await client.PutObjectChunk(containerTemp, objectId, tmpStream.ToArray(), chunk);
                }

                if (progress != null)
                {
                    progress(chunk, bytesRead);
                }

                if (!response.IsSuccess)
                {
                    // cleanup
                    await client.DeleteContainerWithContents(containerTemp);

                    return response;
                }

                chunk++;
            }

            Dictionary<string, string> integrityHeaders = null;

#if !DNXCORE50
            using (var md5 = MD5.Create())
            {
                var eTag = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();

                integrityHeaders = new Dictionary<string, string>() { { "ETag", eTag } };
            }
#endif

            // use manifest to merge chunks
            response = await client.PutManifest(containerTemp, objectId, integrityHeaders);

            if (!response.IsSuccess)
            {
                // cleanup
                await client.DeleteContainerWithContents(containerTemp);

                return response;
            }

            // copy chunks to new file
            response = await client.CopyObject(containerTemp, objectId, containerId, objectId, headers);

            if (!response.IsSuccess)
            {
                // cleanup
                await client.DeleteContainerWithContents(containerTemp);

                return response;
            }

            // cleanup temp
            return await client.DeleteContainerWithContents(containerTemp);
        }

        public static async Task<SwiftBaseResponse> DeleteContainerWithContents(this Client client, string containerId, int limit = 1000)
        {
            // delete all container objects
            var deleteRsp = await client.DeleteContainerContents(containerId, limit);

            if (deleteRsp.IsSuccess)
            {
                //delete container
                return await client.DeleteContainer(containerId);
            }

            return deleteRsp;
        }

        public static async Task<SwiftBaseResponse> DeleteContainerContents(this Client client, string containerId, int limit = 1000)
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
                var infoRsp = await client.GetContainer(containerId, null, queryParams);

                // no more objects => break
                if (infoRsp.ObjectsCount == 0) return infoRsp;

                if (infoRsp.IsSuccess && infoRsp.Objects != null)
                {
                    var objectIds = infoRsp.Objects.Select(x => containerId + "/" + x.Object).ToList();

                    var count = infoRsp.Objects.Count;

                    // delete them
                    var deleteRsp = await client.DeleteObjects(objectIds);

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

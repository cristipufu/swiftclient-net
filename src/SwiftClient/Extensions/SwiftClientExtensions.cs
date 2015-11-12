using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient
{
    public static class SwiftClientExtensions
    {
        public static async Task<SwiftBaseResponse> DeleteContainerWithContents(this SwiftClient client, string containerId, int limit = 1000)
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

        public static async Task<SwiftBaseResponse> DeleteContainerContents(this SwiftClient client, string containerId, int limit = 1000)
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

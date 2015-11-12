using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient
{
    public static class SwiftClientExtensions
    {
        public static async Task<SwiftBaseResponse> DeleteContainerWithObjects(this SwiftClient client, string containerId, int limit = 2)
        {
            var queryParams = new Dictionary<string, string>()
            {
                { "limit", limit.ToString() }
            };

            var marker = string.Empty;

            while (true)
            {
                if (!string.IsNullOrEmpty(marker))
                {
                    if (queryParams.ContainsKey("marker"))
                    {
                        queryParams["marker"] = marker;
                    }
                    else
                    {
                        queryParams.Add("marker", marker);
                    }
                }

                // get objects
                var infoRsp = await client.GetContainer(containerId, null, queryParams);

                // no more objects => break
                if (infoRsp.ObjectsCount == 0) break;

                if (infoRsp.IsSuccess && infoRsp.Objects != null)
                {
                    var objectIds = infoRsp.Objects.Select(x => containerId + "/" + x.Object).ToList();

                    var count = infoRsp.Objects.Count;

                    if (count > 0)
                    {
                        // delete them
                        var deleteRsp = await client.DeleteObjects(objectIds);

                        if (!deleteRsp.IsSuccess) return deleteRsp;
                    }

                    // last page => break
                    if (count < limit) break;

                    marker = infoRsp.Objects.Select(x => x.Object).LastOrDefault();
                }
                else
                {
                    return infoRsp;
                }
            }

            // delete container
            return await client.DeleteContainer(containerId);
        } 
    }
}

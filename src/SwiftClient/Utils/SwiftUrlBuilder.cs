using SwiftClient.Extensions;
using System.Collections.Generic;

namespace SwiftClient.Utils
{
    public static class SwiftUrlBuilder
    {
        public static string GetAccountUrl(string storageUrl, Dictionary<string, string> queryParams = null)
        {
            var url = storageUrl;

            if (queryParams != null)
            {
                url += queryParams.ToQueryString();
            }

            return url;
        }

        public static string GetContainerUrl(string storageUrl, string containerId, Dictionary<string, string> queryParams = null)
        {
            var url = storageUrl + "/" + containerId.Encode();

            if (queryParams != null)
            {
                url += queryParams.ToQueryString();
            }

            return url;
        }

        public static string GetObjectUrl(string storageUrl, string containerId, string objectId, Dictionary<string, string> queryParams = null)
        {
            var url = storageUrl + "/" + containerId.Encode() + "/" + objectId.Encode();

            if (queryParams != null)
            {
                url += queryParams.ToQueryString();
            }

            return url;
        }

        public static string GetAuthUrl(string rootUrl)
        {
            return rootUrl + "/auth/v1.0";
        }
    }
}

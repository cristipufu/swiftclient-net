using SwiftClient.Extensions;

namespace SwiftClient
{
    public static class SwiftHeaderKeys
    {
        public static string AuthUser = "X-Auth-User";
        public static string AuthKey = "X-Auth-Key";
        public static string AuthToken = "X-Auth-Token";
        public static string StorageUrl = "X-Storage-Url";
        public static string ObjectManifest = "X-Object-Manifest";
        public static string ObjectManifestValueFormat = "{0}/{1}";
        public static string CopyFrom = "X-Copy-From";

        public static string AccountObjectCount = "X-Account-Object-Count";
        public static string AccountBytesUsed = "X-Account-Bytes-Used";
        public static string AccountContainerCount = "X-Account-Container-Count";
        public static string AccountMetaFormat = "X-Account-Meta-{0}";
        public static string AccountMeta = "X-Account-Meta";

        public static string ContainerObjectCount = "X-Container-Object-Count";
        public static string ContainerBytesUsed = "X-Container-Bytes-Used";
        public static string ContainerMetaFormat = "X-Container-Meta-{0}";
        public static string ContainerMeta = "X-Container-Meta";

        public static string ObjectMetaFormat = "X-Object-Meta-{0}";
        public static string ObjectMeta = "X-Object-Meta";

        public static string ContentLength = "Content-Length";
        public static string Range = "Range";
        public static string RangeValueFormat = "bytes={0}-{1}";

        public static string GetObjectManifestValue(string containerId, string objectId)
        {
            return string.Format(ObjectManifestValueFormat, containerId.Encode(), objectId.Encode());
        }
    }
}

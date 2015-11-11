
namespace SwiftClient
{
    public static class SwiftResponseExtensions
    {
        public static string GetMeta(this SwiftBaseResponse rsp, string metaName)
        {
            if (rsp.Headers == null) return null;

            var objectMetaKey = string.Format(SwiftHeaderKeys.ObjectMetaFormat, metaName);

            if (rsp.Headers.ContainsKey(objectMetaKey))
            {
                return rsp.Headers[objectMetaKey];
            }

            var containerMetaKey = string.Format(SwiftHeaderKeys.ContainerMetaFormat, metaName);

            if (rsp.Headers.ContainsKey(containerMetaKey))
            {
                return rsp.Headers[containerMetaKey];
            }

            var accountMetaKey = string.Format(SwiftHeaderKeys.AccountMetaFormat, metaName);

            if (rsp.Headers.ContainsKey(accountMetaKey))
            {
                return rsp.Headers[accountMetaKey];
            }

            return null;
        }
    }
}

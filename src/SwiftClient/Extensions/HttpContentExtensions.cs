using System.Collections.Generic;
using System.Net.Http;

namespace SwiftClient.Extensions
{
    internal static class HttpContentExtensions
    {
        public static void SetHeaders(this HttpContent content, Dictionary<string, string> headers)
        {
            if (headers == null) return;
            foreach (var header in headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}

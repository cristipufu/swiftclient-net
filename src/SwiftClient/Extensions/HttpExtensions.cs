using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Headers;

namespace SwiftClient.Extensions
{
    internal static class HttpExtensions
    {
        public static void SetHeaders(this HttpRequestMessage request, Dictionary<string, string> headers = null)
        {
            if (headers == null) return;
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        public static void SetHeaders(this HttpContent content, Dictionary<string, string> headers)
        {
            if (headers == null) return;
            foreach (var header in headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        public static string GetHeader(this HttpResponseMessage rsp, string headerName)
        {
            IEnumerable<string> headers = new List<string>();
            return rsp.Headers.TryGetValues(headerName, out headers) ? headers.FirstOrDefault() : null;
        }

        public static Dictionary<string, string> ToDictionary(this HttpResponseHeaders headers)
        {
            if (headers == null) return null;

            var result = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                result.Add(header.Key, header.Value.FirstOrDefault());
            }

            return result;
        }
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Linq;

namespace SwiftClient.Extensions
{
    public static class HttpMessageExtensions
    {
        public static void SetHeaders(this HttpRequestMessage request, Dictionary<string, string> headers = null)
        {
            if (headers == null) return;
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        public static string GetHeader(this HttpResponseMessage rsp, string headerName)
        {
            IEnumerable<string> headers = new List<string>();
            return rsp.Headers.TryGetValues(headerName, out headers) ? headers.FirstOrDefault() : null;
        }
    }
}

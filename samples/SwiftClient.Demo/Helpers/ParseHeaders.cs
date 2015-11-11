using Microsoft.AspNet.Http;
using System;
using System.Net.Http.Headers;

namespace SwiftClient.Demo
{
    public static class ParseHeaders
    {
        public static RangeHeaderValue GetRange(this HttpContext context)
        {
            var range = context.Request.Headers["Range"];

            var a = range.Count;

            var ranges = range.ToString().Replace("bytes=", "").Split('-');
            long? start = 0, end = null;

            start = long.Parse(ranges[0]);
            if (ranges.Length > 1)
            {
                if (!string.IsNullOrEmpty(ranges[1]))
                {
                    end = long.Parse(ranges[1]);
                }
            }

            return new RangeHeaderValue(start, end);
        }

        public static string GetFileName(this IFormFile file)
        {
            string contentDisposition = file.ContentDisposition;
            string filename = "filename=";
            int index = contentDisposition.LastIndexOf(filename, StringComparison.OrdinalIgnoreCase);
            if (index > -1)
            {
                return contentDisposition.Substring(index + filename.Length).Replace(" ", "-").Replace("\"", "");
            }

            return null;
        }
    }
}

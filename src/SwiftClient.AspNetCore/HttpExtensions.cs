using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;

namespace SwiftClient.AspNetCore
{
    public static class HttpExtensions
    {
        public static RangeHeaderValue GetRanges(this HttpContext context, long contentSize)
        {
            RangeHeaderValue rangesResult = null;

            string rangeHeader = context.Request.Headers["Range"];

            if (!string.IsNullOrEmpty(rangeHeader))
            {
                // rangeHeader contains the value of the Range HTTP Header and can have values like:
                //      Range: bytes=0-1            * Get bytes 0 and 1, inclusive
                //      Range: bytes=0-500          * Get bytes 0 to 500 (the first 501 bytes), inclusive
                //      Range: bytes=400-1000       * Get bytes 500 to 1000 (501 bytes in total), inclusive
                //      Range: bytes=-200           * Get the last 200 bytes
                //      Range: bytes=500-           * Get all bytes from byte 500 to the end
                //
                // Can also have multiple ranges delimited by commas, as in:
                //      Range: bytes=0-500,600-1000 * Get bytes 0-500 (the first 501 bytes), inclusive plus bytes 600-1000 (401 bytes) inclusive

                // Remove "Ranges" and break up the ranges
                string[] ranges = rangeHeader.Replace("bytes=", string.Empty).Split(",".ToCharArray());

                rangesResult = new RangeHeaderValue();

                for (int i = 0; i < ranges.Length; i++)
                {
                    const int START = 0, END = 1;

                    long endByte, startByte;

                    long parsedValue;

                    string[] currentRange = ranges[i].Split("-".ToCharArray());

                    if (long.TryParse(currentRange[END], out parsedValue))
                        endByte = parsedValue;
                    else
                        endByte = contentSize - 1;


                    if (long.TryParse(currentRange[START], out parsedValue))
                        startByte = parsedValue;
                    else
                    {
                        // No beginning specified, get last n bytes of file
                        // We already parsed end, so subtract from total and
                        // make end the actual size of the file
                        startByte = contentSize - endByte;
                        endByte = contentSize - 1;
                    }

                    rangesResult.Ranges.Add(new RangeItemHeaderValue(startByte, endByte));
                }
            }

            return rangesResult;
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

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace SwiftClient.AspNetCore.Demo
{
    public class VideoStreamResult : FileStreamResult
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;
        private string MultipartBoundary = "<qwe123>";

        public VideoStreamResult(Stream fileStream, string contentType)
            : base(fileStream, contentType)
        {
            
        }

        public VideoStreamResult(Stream fileStream, MediaTypeHeaderValue contentType) 
            : base(fileStream, contentType)
        {

        }

        private bool IsMultipartRequest(RangeHeaderValue range)
        {
            return range != null && range.Ranges != null && range.Ranges.Count > 1;
        }

        private bool IsRangeRequest(RangeHeaderValue range)
        {
            return range != null && range.Ranges != null && range.Ranges.Count > 0;
        }

        protected async Task WriteVideoAsync(HttpResponse response)
        {
            var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
            bufferingFeature?.DisableResponseBuffering();

            var length = FileStream.Length;

            var range = response.HttpContext.GetRanges(length);

            if (IsMultipartRequest(range))
            {
                response.ContentType = $"multipart/byteranges; boundary={MultipartBoundary}";
            }
            else
            {
                response.ContentType = ContentType.ToString();
            }

            response.Headers.Add("Accept-Ranges", "bytes");

            if (IsRangeRequest(range))
            {
                response.StatusCode = (int)HttpStatusCode.PartialContent;

                if (!IsMultipartRequest(range))
                {
                    response.Headers.Add("Content-Range", $"bytes {range.Ranges.First().From}-{range.Ranges.First().To}/{length}");
                }

                foreach (var rangeValue in range.Ranges)
                {
                    if (IsMultipartRequest(range)) // dunno if multipart works
                    {
                        await response.WriteAsync($"--{MultipartBoundary}");
                        await response.WriteAsync(Environment.NewLine);
                        await response.WriteAsync($"Content-type: {ContentType}");
                        await response.WriteAsync(Environment.NewLine);
                        await response.WriteAsync($"Content-Range: bytes {range.Ranges.First().From}-{range.Ranges.First().To}/{length}");
                        await response.WriteAsync(Environment.NewLine);
                    }

                    await WriteDataToResponseBody(rangeValue, response);

                    if (IsMultipartRequest(range))
                    {
                        await response.WriteAsync(Environment.NewLine);
                    }
                }

                if (IsMultipartRequest(range))
                {
                    await response.WriteAsync($"--{MultipartBoundary}--");
                    await response.WriteAsync(Environment.NewLine);
                }
            }
            else
            {
                await FileStream.CopyToAsync(response.Body);
            }
        }

        private async Task WriteDataToResponseBody(RangeItemHeaderValue rangeValue, HttpResponse response)
        {
            var startIndex = rangeValue.From ?? 0;
            var endIndex = rangeValue.To ?? 0;

            byte[] buffer = new byte[BufferSize];
            long totalToSend = endIndex - startIndex;
            int count = 0;

            long bytesRemaining = totalToSend + 1;
            response.ContentLength = bytesRemaining;

            FileStream.Seek(startIndex, SeekOrigin.Begin);

            while (bytesRemaining > 0)
            {
                try
                {
                    if (bytesRemaining <= buffer.Length)
                        count = FileStream.Read(buffer, 0, (int)bytesRemaining);
                    else
                        count = FileStream.Read(buffer, 0, buffer.Length);

                    if (count == 0)
                        return;

                    await response.Body.WriteAsync(buffer, 0, count);

                    bytesRemaining -= count;
                }
                catch (IndexOutOfRangeException)
                {
                    await response.Body.FlushAsync();
                    return;
                }
                finally
                {
                    await response.Body.FlushAsync();
                }
            }
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            await WriteVideoAsync(context.HttpContext.Response);
        }
    }

}

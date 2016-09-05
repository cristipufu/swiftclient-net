using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace SwiftClient.AspNetCore
{
    public class VideoStreamResult : FileStreamResult
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;
        private string MultipartBoundary = "c239926cc5b64b";

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

            var rangeHeaderValue = response.HttpContext.GetRanges(length);

            var isMultipart = IsMultipartRequest(rangeHeaderValue);

            if (isMultipart)
            {
                response.ContentType = $"multipart/byteranges; boundary={MultipartBoundary}";
            }
            else
            {
                response.ContentType = ContentType.ToString();
            }

            response.Headers.Add("Accept-Ranges", "bytes");

            if (IsRangeRequest(rangeHeaderValue))
            {
                response.StatusCode = (int)HttpStatusCode.PartialContent;

                foreach (var range in rangeHeaderValue.Ranges)
                {
                    if (isMultipart)
                    {
                        await response.WriteAsync($"--{MultipartBoundary}{Environment.NewLine}");
                        await response.WriteAsync($"Content-type: {ContentType}{Environment.NewLine}");
                        await response.WriteAsync($"Content-Range: bytes {range.From}-{range.To}/{length}{Environment.NewLine}");
                    }
                    else
                    {
                        response.Headers.Add("Content-Range", $"bytes {range.From}-{range.To}/{length}");
                    }

                    await WriteDataToResponseBody(response, range);

                    if (isMultipart)
                    {
                        await response.WriteAsync($"{Environment.NewLine}--{MultipartBoundary}--{Environment.NewLine}");
                    }
                }
            }
            else
            {
                await FileStream.CopyToAsync(response.Body);
            }
        }

        private async Task WriteDataToResponseBody(HttpResponse response, RangeItemHeaderValue rangeValue)
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

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Net.Http.Headers;

namespace SwiftClient.Demo
{
    public class VideoStreamResult : FileResult
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;
        private Stream _videoStream;
        private string MultipartBoundary = "<qwe123>";

        /// <summary>
        /// Creates a new <see cref="VideoStreamResult"/> instance with
        /// the provided <paramref name="stream"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="stream">The stream with the file.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VideoStreamResult(Stream stream, string contentType)
            : this(stream, new MediaTypeHeaderValue(contentType))
        {
        }

        /// <summary>
        /// Creates a new <see cref="VideoStreamResult"/> instance with
        /// the provided <paramref name="stream"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="stream">The stream with the file.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VideoStreamResult(Stream stream, MediaTypeHeaderValue contentType)
            : base(contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            VideoStream = stream;
        }

        /// <summary>
        /// Gets or sets the stream with the file that will be sent back as the response.
        /// </summary>
        public Stream VideoStream
        {
            get
            {
                return _videoStream;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _videoStream = value;
            }
        }

        private bool IsMultipartRequest(RangeHeaderValue range)
        {
            return range != null && range.Ranges != null && range.Ranges.Count > 1;
        }

        private bool IsRangeRequest(RangeHeaderValue range)
        {
            return range != null && range.Ranges != null && range.Ranges.Count > 0;
        }

        protected async Task WriteVideoAsync(HttpResponse response, CancellationToken cancellation)
        {
            var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
            bufferingFeature?.DisableResponseBuffering();

            var length = VideoStream.Length;

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
                        await response.WriteAsync($"Content-type: {ContentType.MediaType}");
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
                await VideoStream.CopyToAsync(response.Body);
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

            VideoStream.Seek(startIndex, SeekOrigin.Begin);

            while (bytesRemaining > 0)
            {
                try
                {
                    if (bytesRemaining <= buffer.Length)
                        count = VideoStream.Read(buffer, 0, (int)bytesRemaining);
                    else
                        count = VideoStream.Read(buffer, 0, buffer.Length);

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

        protected override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
        {
            return WriteVideoAsync(response, cancellation);
        }
    }

}

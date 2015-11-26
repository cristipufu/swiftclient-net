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
        private string MultipartBoundary = "<q1w2e3r4t5y6u7i8o9p0>";

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
            var length = VideoStream.Length;

            var range = response.HttpContext.GetRanges(length);

            if (range != null && range.Ranges != null && range.Ranges.Count > 1)
            {
                response.ContentType = string.Format("multipart/byteranges; boundary={0}", MultipartBoundary);
            }
            else
            {
                response.ContentType = ContentType.ToString();
            }

            response.Headers.Add("Accept-Ranges", "bytes");

            if (IsRangeRequest(range))
            {
                response.StatusCode = (int)HttpStatusCode.PartialContent;

                response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}",
                        range.Ranges.First().From,
                        range.Ranges.First().To,
                        length));

            }

            var outputStream = response.Body;

            using (VideoStream)
            {
                var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
                bufferingFeature?.DisableResponseBuffering();

                await VideoStream.CopyToAsync(outputStream, BufferSize, cancellation);
            }
        }

        protected override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
        {
            return WriteVideoAsync(response, cancellation);
        }
    }

}

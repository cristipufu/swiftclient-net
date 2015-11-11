using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SwiftClient.Demo
{
    public class VideoStreamResult : FileResult
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;
        private Stream _videoStream;

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

        /// <inheritdoc />
        protected async Task WriteVideoAsync(HttpResponse response, CancellationToken cancellation)
        {
            var ranges = response.HttpContext.GetRange();

            var length = VideoStream.Length;

            if (ranges.Ranges.Count > 0)
            {
                var range = ranges.Ranges.First();

                response.Headers.Add("Content-Length", (range.To ?? length - range.From).ToString());
                response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", 
                    range.From, 
                    range.To.HasValue ? range.To - 1 : null, 
                    range.To.HasValue ? range.To : length - range.From));
                response.Headers.Add("Expires", "-1");
                response.Headers.Add("Cache-Control", "no-cache");
                response.Headers.Add("Accept-Ranges", "bytes");
            }

            response.StatusCode = (int)HttpStatusCode.PartialContent;

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

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SwiftClient.Test.Utils
{
    public static class VideoStreamHelper
    {
        public static async Task<TestResponse> GetFromHttp(HttpClient httpClient, string videoPath, long? from = null, long? to = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, videoPath);

            if (!from.HasValue)
            {
                from = 0;
            }

            request.Headers.Range = new RangeHeaderValue(from, to);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                var videoStream = await response.Content.ReadAsStreamAsync();

                var ms = new MemoryStream();

                videoStream.CopyTo(ms);

                var result = new TestResponse()
                {
                    StatusCode = response.StatusCode,
                    Length = response.Content.Headers.ContentLength ?? 0,
                    Bytes = ms.ToArray()
                };

                response.Dispose();

                return result;
            }

            return null;
        }

        public static async Task UploadVideo(Client client, string containerId, string videoId, int maxBufferSize)
        {
            var containerResponse = await client.PutContainerAsync(containerId);

            if (containerResponse.IsSuccess)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
                var data = generator.GenerateBufferFromSeed(maxBufferSize);

                // upload
                await client.PutObjectAsync(containerId, videoId, data);
            }
        }

        public static async Task<TestResponse> GetFromSwift(Client client, string containerId, string videoId, long? from = null, long? to = null)
        {
            SwiftResponse resp;

            if (!from.HasValue && !to.HasValue)
            {
                resp = await client.GetObjectAsync(containerId, videoId);
            }
            else
            {
                if (!from.HasValue)
                {
                    from = 0;
                }

                if (!to.HasValue)
                {
                    var obj = await client.HeadObjectAsync(containerId, videoId);

                    to = obj.ContentLength;
                }

                resp = await client.GetObjectRangeAsync(containerId, videoId, from.Value, to.Value);
            }

            var ms = new MemoryStream();

            resp.Stream.CopyTo(ms);

            var result = new TestResponse()
            {
                Length = resp.ContentLength,
                Bytes = ms.ToArray()
            };

            return result;
        }

        public static bool CompareBytes(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length) return false;

            return b1.SequenceEqual(b2);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SwiftClient.Test
{
    public class VideoStreamTests : IClassFixture<SwiftFixtureDemo<AspNetCore.Demo.Startup>>
    {
        #region Ctor and Properties
        private const string videoPathFormat = "/home/playvideo?containerId={0}&objectId={1}";
        private const string videoId = "videoFile.mp4";

        SwiftFixtureDemo<AspNetCore.Demo.Startup> fixture;
        ITestOutputHelper output;

        public HttpClient HttpClient
        {
            get
            {
                return fixture.HttpClient;
            }
        }

        string videoPath
        {
            get
            {
                return string.Format(videoPathFormat, containerId, videoId);
            }
        }

        string containerId
        {
            get
            {
                return fixture.ContainerId;
            }
        }

        SwiftCredentials credentials
        {
            get
            {
                return fixture.Credentials;
            }
        }

        public Client GetClient()
        {
            var client = new Client(credentials);

            client.SetLogger(new SwiftLogger(output));
            client.SetRetryCount(2);

            return client;
        }

        public VideoStreamTests(SwiftFixtureDemo<AspNetCore.Demo.Startup> fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.output = output;
        }

        #endregion

        [Fact]
        public async Task Rangeless()
        {
            using (var client = GetClient())
            {
                await UploadVideo(client);

                var videoResp = await GetFromHttp();

                var swiftResp = await GetFromSwift(client);

                // status code
                Assert.True(videoResp.StatusCode == HttpStatusCode.OK || videoResp.StatusCode == HttpStatusCode.PartialContent);
                // content bytes
                Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
            }
        }

        //Range: bytes=4718592-
        [Fact]
        public async Task RangeFrom()
        {
            using (var client = GetClient())
            {
                await UploadVideo(client);

                var from = new Random().Next(fixture.MaxBufferSize);

                var videoResp = await GetFromHttp(from);

                var swiftResp = await GetFromSwift(client, from);

                // status code
                Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
                // content length headers
                Assert.True(videoResp.Length == swiftResp.Length);
                // content bytes
                Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
            }
        }

        //Range: bytes=-4718592
        [Fact]
        public async Task RangeTo()
        {
            using (var client = GetClient())
            {
                await UploadVideo(client);

                var to = new Random().Next(fixture.MaxBufferSize);

                var videoResp = await GetFromHttp(to: to);

                var swiftResp = await GetFromSwift(client, to: to);

                // Fails 
                // TODO fix 1 extra byte on swiftResp

                // status code
                Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
                // content length headers
                Assert.True(videoResp.Length == swiftResp.Length);
                // content bytes
                Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
            }
        }

        //Range: bytes=20234240-24379391
        [Fact]
        public async Task Range()
        {
            using (var client = GetClient())
            {
                await UploadVideo(client);

                var to = new Random().Next(fixture.MaxBufferSize);

                var from = new Random().Next(to);

                var videoResp = await GetFromHttp(from, to);

                var swiftResp = await GetFromSwift(client, from, to);

                // status code
                Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
                // content length headers
                Assert.True(videoResp.Length == swiftResp.Length);
                // content bytes
                Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
            }
        }

        #region Helpers

        private async Task<TestResponse> GetFromHttp(long? from = null, long? to = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, videoPath);

            if (from.HasValue || to.HasValue)
            {
                request.Headers.Range = new RangeHeaderValue(from, to);
            }

            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

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

        private async Task UploadVideo(Client client)
        {
            var containerResponse = await client.PutContainer(containerId);

            if (containerResponse.IsSuccess)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(fixture.MaxBufferSize);
                var data = generator.GenerateBufferFromSeed(fixture.MaxBufferSize);

                // upload
                var createRsp = await client.PutObject(containerId, videoId, data);
            }

        }

        private async Task<TestResponse> GetFromSwift(Client client, long? from = null, long? to = null)
        {
            SwiftResponse resp;

            if (!from.HasValue && !to.HasValue)
            {
                resp = await client.GetObject(containerId, videoId);
            }
            else
            {
                if (!from.HasValue)
                {
                    from = 0;
                }

                if (!to.HasValue)
                {
                    var obj = await client.HeadObject(containerId, videoId);

                    to = obj.ContentLength;
                }

                resp = await client.GetObjectRange(containerId, videoId, from.Value, to.Value);
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


        private class TestResponse
        {
            public byte[] Bytes { get; set; }

            public long Length { get; set; }

            public HttpStatusCode StatusCode { get; set; }
        }

        private bool CompareBytes(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length) return false;

            return b1.SequenceEqual(b2);
        }

        #endregion
    }
}

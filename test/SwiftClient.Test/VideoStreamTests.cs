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
            var videoResp = await GetFromHttp();

            var swiftResp = await GetFromSwift();

            // status code
            Assert.True(videoResp.StatusCode == HttpStatusCode.OK || videoResp.StatusCode == HttpStatusCode.PartialContent);
            // content length headers
            Assert.True(videoResp.Length == swiftResp.Length);
            // content bytes
            Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        //Range: bytes=4718592-
        [Fact]
        public async Task RangeFrom()
        {
            var videoLength = await GetVideoLength();

            var from = new Random().Next((int)videoLength);

            var videoResp = await GetFromHttp(from);

            var swiftResp = await GetFromSwift(from);

            // status code
            Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
            // content length headers
            Assert.True(videoResp.Length == swiftResp.Length);
            // content bytes
            Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        //Range: bytes=-4718592
        [Fact]
        public async Task RangeTo()
        {
            var videoLength = await GetVideoLength();

            var to = new Random().Next((int)videoLength);

            var videoResp = await GetFromHttp(to: to);

            var swiftResp = await GetFromSwift(to: to);

            // status code
            Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
            // content length headers
            Assert.True(videoResp.Length == swiftResp.Length);
            // content bytes
            Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        //Range: bytes=20234240-24379391
        [Fact]
        public async Task Range()
        {
            var videoLength = await GetVideoLength();

            var to = new Random().Next((int)videoLength);

            var from = new Random().Next(to);

            var videoResp = await GetFromHttp(from, to);

            var swiftResp = await GetFromSwift(from, to);

            // status code
            Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
            // content length headers
            Assert.True(videoResp.Length == swiftResp.Length);
            // content bytes
            Assert.True(CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        #region Helpers

        private async Task<TestResponse> GetFromHttp(long? from = null, long? to = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, videoPath);
            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (from.HasValue || to.HasValue)
            {
                request.Headers.Range = new RangeHeaderValue(from, to);
            }

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

        private async Task<TestResponse> GetFromSwift(long? from = null, long? to = null)
        {
            using (var client = GetClient())
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
        }

        private async Task<long> GetVideoLength()
        {
            using (var client = GetClient())
            {
                var obj = await client.HeadObject(containerId, videoId);

                return obj.ContentLength;
            }
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

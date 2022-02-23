using SwiftClient.Test.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SwiftClient.Test
{
    public class VideoStreamTests : IClassFixture<SwiftFixtureDemo<AspNetCore.Demo.Startup>>
    {
        #region Ctor and Properties

        private readonly SwiftFixtureDemo<AspNetCore.Demo.Startup> _fixture;
        private readonly ITestOutputHelper _output;

        private HttpClient HttpClient => _fixture.HttpClient;

        private IHttpClientFactory HttpClientFactory => _fixture.HttpClientFactory;

        private string VideoPathFormat => _fixture.VideoPathFormat;

        private string VideoId => _fixture.VideoId;

        private string VideoPath => string.Format(VideoPathFormat, ContainerId, VideoId);

        private string ContainerId => _fixture.ContainerId;

        private int MaxBufferSize => _fixture.MaxBufferSize;

        private SwiftCredentials credentials => _fixture.Credentials;

        public Client GetClient()
        {
            var client = new Client(credentials);

            client.SetHttpClient(HttpClientFactory);
            client.SetLogger(new SwiftLogger(_output));
            client.SetRetryCount(2);

            return client;
        }

        public VideoStreamTests(SwiftFixtureDemo<AspNetCore.Demo.Startup> fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        #endregion

        [Fact]
        public async Task Rangeless()
        {
            using var client = GetClient();

            await VideoStreamHelper.UploadVideo(client, ContainerId, VideoId, MaxBufferSize);
            var videoResp = await VideoStreamHelper.GetFromHttp(HttpClient, VideoPath);
            var swiftResp = await VideoStreamHelper.GetFromSwift(client, ContainerId, VideoId);

            // status code
            Assert.True(videoResp.StatusCode == HttpStatusCode.OK || videoResp.StatusCode == HttpStatusCode.PartialContent);
            // content bytes
            Assert.True(VideoStreamHelper.CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        //Range: bytes=4718592-
        [Fact]
        public async Task RangeFrom()
        {
            using var client = GetClient();

            await VideoStreamHelper.UploadVideo(client, ContainerId, VideoId, MaxBufferSize);
            var from = new Random().Next(_fixture.MaxBufferSize);
            var videoResp = await VideoStreamHelper.GetFromHttp(HttpClient, VideoPath, from);
            var swiftResp = await VideoStreamHelper.GetFromSwift(client, ContainerId, VideoId, from);

            // status code
            Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
            // content length headers
            Assert.True(videoResp.Length == swiftResp.Length);
            // content bytes
            Assert.True(VideoStreamHelper.CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        //Range: bytes=-4718592
        [Fact]
        public async Task RangeTo()
        {
            using var client = GetClient();

            await VideoStreamHelper.UploadVideo(client, ContainerId, VideoId, MaxBufferSize);
            var to = new Random().Next(_fixture.MaxBufferSize);
            var videoResp = await VideoStreamHelper.GetFromHttp(HttpClient, VideoPath, to: to);
            var swiftResp = await VideoStreamHelper.GetFromSwift(client, ContainerId, VideoId, to: to);

            // Fails 
            // TODO fix 1 extra byte on swiftResp

            Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
            Assert.True(videoResp.Length == swiftResp.Length);
            Assert.True(VideoStreamHelper.CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

        //Range: bytes=20234240-24379391
        [Fact]
        public async Task Range()
        {
            using var client = GetClient();

            await VideoStreamHelper.UploadVideo(client, ContainerId, VideoId, MaxBufferSize);
            var to = new Random().Next(_fixture.MaxBufferSize);
            var from = new Random().Next(to);
            var videoResp = await VideoStreamHelper.GetFromHttp(HttpClient, VideoPath, from, to); ;
            var swiftResp = await VideoStreamHelper.GetFromSwift(client, ContainerId, VideoId, from, to);

            Assert.True(videoResp.StatusCode == HttpStatusCode.PartialContent);
            Assert.True(videoResp.Length == swiftResp.Length);
            Assert.True(VideoStreamHelper.CompareBytes(videoResp.Bytes, swiftResp.Bytes));
        }

    }
}

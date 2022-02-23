using SwiftClient.Test.Utils;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace SwiftClient.Test
{
    public class SwiftClientTests : IClassFixture<SwiftFixture>
    {
        #region Ctor and Properties

        private readonly SwiftFixture _fixture;
        private readonly ITestOutputHelper _output;

        private int MaxBufferSize => _fixture.MaxBufferSize;

        private int Chunks => _fixture.Chunks; 

        private string ContainerId => _fixture.ContainerId; 

        private string PseudoDirectoryId => _fixture.ObjectId;

        private string ObjectId => _fixture.ObjectId; 

        private string ChunkedObjectId => _fixture.ChunkedObjectId; 

        private SwiftCredentials Credentials => _fixture.Credentials;

        private IHttpClientFactory HttpClientFactory => _fixture.HttpClientFactory;

        public SwiftClientTests(SwiftFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        #endregion

        public Client GetClient()
        {
            var client = new Client(Credentials);

            client.SetLogger(new SwiftLogger(_output));
            client.SetHttpClient(HttpClientFactory);
            client.SetRetryCount(2);

            return client;
        }

        [Fact]
        public async Task AuthenticateTest()
        {
            using var client = GetClient();
            var rsp = await client.AuthenticateAsync();

            Assert.True(rsp != null && !string.IsNullOrEmpty(rsp.AuthToken));
        }

        [Fact]
        public async Task PutContainerTest()
        {
            using var client = GetClient();
            await client.PutContainerAsserts(ContainerId);
        }


        [Fact]
        public async Task PutPseudoDirectoryTest()
        {
            using var client = GetClient();
            await client.PutContainerAsserts(ContainerId);
            await client.PutPseudoDirectoryAsserts(ContainerId, PseudoDirectoryId);
        }

        [Fact]
        public async Task PutObjectTest()
        {
            using var client = GetClient();
            await client.PutContainerAsserts(ContainerId);
            await client.PutObjectAsserts(ContainerId, ObjectId, MaxBufferSize);
        }

        [Fact]
        public async Task PutChunkedObjectTest()
        {
            using var client = GetClient();
            await client.PutObjectAsserts(ContainerId, ObjectId, MaxBufferSize);
            await client.PutChunkedObjectAsserts(ContainerId, ChunkedObjectId, Chunks, MaxBufferSize);
        }

        [Fact]
        public async Task GetAccountTest()
        {
            using var client = GetClient();
            await client.PutObjectAsserts(ContainerId, ObjectId, MaxBufferSize);
            await client.GetAccountAsserts(ContainerId);
        }

        [Fact]
        public async Task GetContainerTest()
        {
            using var client = GetClient();
            await client.PutContainerAsserts(ContainerId);
            await client.PutObjectAsserts(ContainerId, ObjectId, MaxBufferSize);
            await client.GetContainerAsserts(ContainerId, MaxBufferSize);
        }
    }
}

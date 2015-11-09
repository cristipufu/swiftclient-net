using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Framework.Configuration;

using Newtonsoft.Json;
using Xunit;

namespace SwiftClient.Test
{
    public class SwiftInitFixture
    {
        public ISwiftClient Client;
        public string ContainerId = "testcontainer";
        public string ObjectId = "testfile";
        public string ChunckedObjectId = "testfilechunks";
        public int MaxBufferSize = 10 * 1024;

        public SwiftInitFixture()
        {
            var credentials = GetConfigCredentials();

            if (Client == null)
            {
                Client = new SwiftClient()
                    .WithCredentials(credentials)
                    .SetRetryCount(2)
                    .SetLogger(new SwiftLogger());
            }
        }

        SwiftCredentials GetConfigCredentials()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("xunit.runner.json");

            var configuration = builder.Build();

            var section = configuration.GetSection("Credentials");

            return new SwiftCredentials()
            {
                Username = section["Username"],
                Password = section["Password"],
                Endpoints = section["Endpoints"].Split(',').ToList()
            };
        }
    }

    public class Tests : IClassFixture<SwiftInitFixture>
    {
        #region Ctor and Properties

        SwiftInitFixture fixture;
        ISwiftClient client
        {
            get
            {
                return fixture.Client;
            }
        }
        string containerId
        {
            get
            {
                return fixture.ContainerId;
            }
        }
        string objectId
        {
            get
            {
                return fixture.ObjectId;
            }
        }

        string chunkedObjectId
        {
            get
            {
                return fixture.ChunckedObjectId;
            }
        }

        int maxBufferSize
        {
            get
            {
                return fixture.MaxBufferSize;
            }
        }

        public Tests(SwiftInitFixture fixture)
        {
            this.fixture = fixture;
        }

        #endregion

        [Fact()]
        public async Task AuthenticateTest()
        {
            // auth
            var rsp = await client.Authenticate();

            Assert.True(rsp != null && !string.IsNullOrEmpty(rsp.AuthToken));
        }

        [Fact()]
        public async Task PutContainerTest()
        {
            // auth
            await AuthenticateTest();

            // create
            var createRsp = await client.PutContainer(containerId);

            Assert.True(createRsp.IsSuccess);

            // exists
            var existsRsp = await client.HeadContainer(containerId);

            Assert.True(existsRsp.IsSuccess);
        }

        [Fact()]
        public async Task PutObjectTest()
        {
            // auth
            await AuthenticateTest();

            // upload container
            await PutContainerTest();

            // generate random byte array
            RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
            var data = generator.GenerateBufferFromSeed(maxBufferSize);

            // upload
            var createRsp = await client.PutObject(containerId, objectId, data);

            Assert.True(createRsp.IsSuccess);

            // exists
            var existsRsp = await client.HeadObject(containerId, objectId);

            Assert.True(existsRsp.IsSuccess && existsRsp.ContentLength == maxBufferSize);

            // get
            var getRsp = await client.GetObject(containerId, objectId);

            Assert.True(getRsp.IsSuccess && getRsp.Stream != null && getRsp.Stream.Length == maxBufferSize);
        }

        [Fact()]
        public async Task PutChunkedObjectTest()
        {
            // auth
            await AuthenticateTest();

            // upload container
            await PutContainerTest();

            var chunks = 10;

            // upload chunks
            for (var i = 0; i < chunks; i++)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
                var data = generator.GenerateBufferFromSeed(maxBufferSize);

                // upload
                var createRsp = await client.PutChunkedObject(containerId, chunkedObjectId, data, i);

                Assert.True(createRsp.IsSuccess);
            }

            // upload manifest
            var manifestResp = await client.PutManifest(containerId, chunkedObjectId);

            Assert.True(manifestResp.IsSuccess);

            // exists
            var existsRsp = await client.HeadObject(containerId, chunkedObjectId);

            Assert.True(existsRsp.IsSuccess && existsRsp.ContentLength == maxBufferSize * chunks);

            // get object
            var getRsp = await client.GetObject(containerId, chunkedObjectId);

            Assert.True(getRsp.IsSuccess && getRsp.Stream != null && getRsp.Stream.Length == maxBufferSize * chunks);

            // get chunk
            var chunkResp = await client.GetObjectRange(containerId, chunkedObjectId, 0, maxBufferSize - 1);

            Assert.True(chunkResp.IsSuccess && chunkResp.Stream != null && chunkResp.Stream.Length == maxBufferSize);
        }

        [Fact()]
        public async Task GetAccountTest()
        {
            // auth
            await AuthenticateTest();

            // upload container
            await PutContainerTest();

            var resp = await client.GetAccount(new Dictionary<string, string>() { { "format", "json" } });

            Assert.True(resp.IsSuccess);

            var containersList = JsonConvert.DeserializeObject<List<ContainerInfoModel>>(resp.Info);

            Assert.True(containersList.Any(x => x.name == containerId));
        }

        [Fact()]
        public async Task GetContainerTest()
        {
            // auth
            await AuthenticateTest();

            // upload container
            await PutContainerTest();

            // upload object
            await PutObjectTest();

            var resp = await client.GetContainer(containerId, null, new Dictionary<string, string>() { { "format", "json" } });

            Assert.True(resp.IsSuccess);

            var objectsList = JsonConvert.DeserializeObject<List<ObjectInfoModel>>(resp.Info);

            Assert.True(objectsList.Count > 0 && objectsList[0].bytes == maxBufferSize);
        }
    }
}

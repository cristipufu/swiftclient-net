using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Framework.Configuration;

using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Framework.DependencyInjection;

namespace SwiftClient.Test
{
    public class SwiftInitFixture
    {
        public SwiftCredentials Credentials;

        public SwiftInitFixture()
        {
            Credentials = GetConfigCredentials();   
        }

        SwiftCredentials GetConfigCredentials()
        {
            var services = new ServiceCollection();
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configuration = builder.Build();

            var section = configuration.GetSection("Credentials");

            return CustomConfigReader.Get<SwiftCredentials>(section);
        }
    }

    public class Tests : IClassFixture<SwiftInitFixture>
    {
        #region Ctor and Properties

        public string containerId = "testcontainer";
        public string objectId = "testfile";
        public string chunkedObjectId = "testfilechunks";
        public int maxBufferSize = 10 * 1024;

        SwiftInitFixture fixture;
        ITestOutputHelper output;

        SwiftCredentials credentials
        {
            get
            {
                return fixture.Credentials;
            }
        }

        public Tests(SwiftInitFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.output = output;
        }

        #endregion

        public SwiftClient GetClient()
        {
            var client = new SwiftClient(credentials);

            client.SetLogger(new SwiftLogger(output));
            client.SetRetryCount(2);

            return client;
        }

        [Fact]
        public async Task AuthenticateTest()
        {
            using(var client = GetClient())
            {
                await Authenticate(client);
            }
        }

        public async Task Authenticate(SwiftClient client)
        {
            // auth
            var rsp = await client.Authenticate();

            Assert.True(rsp != null && !string.IsNullOrEmpty(rsp.AuthToken));
        }

        [Fact]
        public async Task PutContainerTest()
        {
            using (var client = GetClient())
            {
                await Authenticate(client);

                await PutContainer(client);
            }
        }

        public async Task PutContainer(SwiftClient client)
        {
            // create
            var createRsp = await client.PutContainer(containerId);

            Assert.True(createRsp.IsSuccess);

            // exists
            var existsRsp = await client.HeadContainer(containerId);

            Assert.True(existsRsp.IsSuccess);
        }

        [Fact]
        public async Task PutObjectTest()
        {
            using (var client = GetClient())
            {
                await Authenticate(client);

                await PutContainer(client);

                await PutObject(client);
            }
        }

        public async Task PutObject(SwiftClient client)
        {
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

        [Fact]
        public async Task PutChunkedObjectTest()
        {
            using (var client = GetClient())
            {
                await Authenticate(client);

                await PutContainer(client);

                await PutChunkedObject(client);
            }
        }

        public async Task PutChunkedObject(SwiftClient client)
        {
            var chunks = 10;
            var tasks = new List<Task>();

            // upload chunks
            for (var i = 0; i < chunks; i++)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
                var data = generator.GenerateBufferFromSeed(maxBufferSize);

                var task = client.PutChunkedObject(containerId, chunkedObjectId, data, i);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

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

        [Fact]
        public async Task GetAccountTest()
        {
            using (var client = GetClient())
            {
                await Authenticate(client);

                await PutContainer(client);

                await GetAccount(client);
            }
        }

        public async Task GetAccount(SwiftClient client)
        {
            var resp = await client.GetAccount(new Dictionary<string, string>() { { "format", "json" } });

            Assert.True(resp.IsSuccess);

            var containersList = JsonConvert.DeserializeObject<List<ContainerInfoModel>>(resp.Info);

            Assert.True(containersList.Any(x => x.name == containerId));
        }

        [Fact]
        public async Task GetContainerTest()
        {
            using (var client = GetClient())
            {
                await Authenticate(client);

                await PutContainer(client);

                await PutObject(client);

                await GetContainer(client);
            }
        }

        public async Task GetContainer(SwiftClient client)
        {
            var resp = await client.GetContainer(containerId, null, new Dictionary<string, string>() { { "format", "json" } });

            Assert.True(resp.IsSuccess);

            var objectsList = JsonConvert.DeserializeObject<List<ObjectInfoModel>>(resp.Info);

            Assert.True(objectsList.Count > 0 && objectsList[0].bytes == maxBufferSize);
        }
    }
}

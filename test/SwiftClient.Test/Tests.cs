using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Framework.Configuration;

using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Framework.DependencyInjection;
using System;

namespace SwiftClient.Test
{
    public class SwiftInitFixture : IDisposable
    {
        public SwiftCredentials Credentials;
        public string ContainerId = "testcontainer";
        public string ObjectId = "testfile";
        public string ChunkedObjectId = "testfilechunks";
        public int MaxBufferSize = 10 * 1024;
        public int Chunks = 10;

        public SwiftInitFixture()
        {
            Credentials = GetConfigCredentials();   
        }

        public void Dispose()
        {
            using (var client = new SwiftClient(Credentials))
            {
                var deleteFilesTasks = new List<Task>();

                deleteFilesTasks.Add(client.DeleteObject(ContainerId, ObjectId));
                deleteFilesTasks.Add(client.DeleteObject(ContainerId, ChunkedObjectId));

                for (var i = 0; i < Chunks; i++)
                {
                    deleteFilesTasks.Add(client.DeleteObjectChunk(ContainerId, ChunkedObjectId, i));
                }

                var delContainerTask = client.DeleteContainer(ContainerId);

                Task.WhenAll(deleteFilesTasks)
                .ContinueWith((rsp) => delContainerTask)
                .Wait();
            }
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
        
        SwiftInitFixture fixture;
        ITestOutputHelper output;

        int maxBufferSize
        {
            get
            {
                return fixture.MaxBufferSize;
            }
        }

        int Chunks
        {
            get
            {
                return fixture.Chunks;
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
                return fixture.ChunkedObjectId;
            }
        }

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
                await PutContainer(client);

                await PutChunkedObject(client);
            }
        }

        public async Task PutChunkedObject(SwiftClient client)
        {
            var tasks = new List<Task>();

            // upload chunks
            for (var i = 0; i < Chunks; i++)
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

            Assert.True(existsRsp.IsSuccess && existsRsp.ContentLength == maxBufferSize * Chunks);

            // get object
            var getRsp = await client.GetObject(containerId, chunkedObjectId);

            Assert.True(getRsp.IsSuccess && getRsp.Stream != null && getRsp.Stream.Length == maxBufferSize * Chunks);

            // get chunk
            var chunkResp = await client.GetObjectRange(containerId, chunkedObjectId, 0, maxBufferSize - 1);

            Assert.True(chunkResp.IsSuccess && chunkResp.Stream != null && chunkResp.Stream.Length == maxBufferSize);
        }

        [Fact]
        public async Task GetAccountTest()
        {
            using (var client = GetClient())
            {
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

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace SwiftClient.Test
{
    public class SwiftInitFixture : IDisposable
    {
        public SwiftCredentials Credentials;
        public string ContainerId = "testcontainer";
        public string PseudoDirectoryId = "pseudodirectory";
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
            using (var client = new Client(Credentials))
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

        string pseudoDirectoryId
        {
            get
            {
                return fixture.ObjectId;
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

        public Client GetClient()
        {
            var client = new Client(credentials);

            client.SetLogger(new SwiftLogger(output));
            client.SetRetryCount(2);

            return client;
        }

        [Fact]
        public async Task AuthenticateTest()
        {
            using (var client = GetClient())
            {
                await Authenticate(client);
            }
        }

        public async Task Authenticate(Client client)
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

        public async Task PutContainer(Client client)
        {
            // create
            var createRsp = await client.PutContainer(containerId);

            Assert.True(createRsp.IsSuccess);

            // exists
            var existsRsp = await client.HeadContainer(containerId);

            Assert.True(existsRsp.IsSuccess);
        }

        [Fact]
        public async Task PutPseudoDirectoryTest()
        {
            using (var client = GetClient())
            {
                await PutContainer(client);

                await PutPseudoDirectory(client);
            }
        }

        public async Task PutPseudoDirectory(Client client)
        {
            var createRsp = await client.PutPseudoDirectory(containerId, pseudoDirectoryId);

            Assert.True(createRsp.IsSuccess);

            var get = await client.GetObject(containerId, pseudoDirectoryId);

            using (var getRsp = await client.GetObject(containerId, pseudoDirectoryId))
            {
                Assert.True(getRsp.IsSuccess);
            }
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

        public async Task PutObject(Client client)
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
            using (var getRsp = await client.GetObject(containerId, objectId))
            {
                Assert.True(getRsp.IsSuccess && getRsp.Stream != null);

                using (var ms = new MemoryStream())
                {
                    await getRsp.Stream.CopyToAsync(ms);

                    Assert.True(ms.Length == maxBufferSize);
                }
            }
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

        public async Task PutChunkedObject(Client client)
        {
            var tasks = new List<Task>();

            // upload chunks
            for (var i = 0; i < Chunks; i++)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
                var data = generator.GenerateBufferFromSeed(maxBufferSize);

                var task = client.PutObjectChunk(containerId, chunkedObjectId, data, i);

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
            using (var getRsp = await client.GetObject(containerId, chunkedObjectId))
            {
                Assert.True(getRsp.IsSuccess && getRsp.Stream != null);

                using (var ms = new MemoryStream())
                {
                    await getRsp.Stream.CopyToAsync(ms);

                    Assert.True(ms.Length == maxBufferSize * Chunks);
                }
            }

            // get chunk
            using (var chunkResp = await client.GetObjectRange(containerId, chunkedObjectId, 0, maxBufferSize - 1))
            {
                Assert.True(chunkResp.IsSuccess && chunkResp.Stream != null);

                using (var ms = new MemoryStream())
                {
                    await chunkResp.Stream.CopyToAsync(ms);

                    Assert.True(ms.Length == maxBufferSize);
                }
            }
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

        public async Task GetAccount(Client client)
        {
            var resp = await client.GetAccount();

            Assert.True(resp.IsSuccess);

            Assert.True(resp.Containers.Any(x => x.Container == containerId));
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

        public async Task GetContainer(Client client)
        {
            var resp = await client.GetContainer(containerId);

            Assert.True(resp.IsSuccess);

            Assert.True(resp.ObjectsCount > 0 && resp.Objects[0].Bytes == maxBufferSize);
        }
    }
}

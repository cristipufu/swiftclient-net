using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SwiftClient.Test.Utils
{
    public static class ClientTestExtensions
    {
        public static async Task PutContainerAsserts(this Client client, string containerId)
        {
            // create
            var createRsp = await client.PutContainerAsync(containerId);

            Assert.True(createRsp.IsSuccess);

            // exists
            var existsRsp = await client.HeadContainerAsync(containerId);

            Assert.True(existsRsp.IsSuccess);
        }

        public static async Task GetContainerAsserts(this Client client, string containerId, int maxBufferSize)
        {
            var resp = await client.GetContainerAsync(containerId);

            Assert.True(resp.IsSuccess);

            Assert.True(resp.ObjectsCount > 0 && resp.Objects[0].Bytes == maxBufferSize);
        }

        public static async Task GetAccountAsserts(this Client client, string containerId)
        {
            var resp = await client.GetAccountAsync();

            Assert.True(resp.IsSuccess);

            Assert.Contains(resp.Containers, x => x.Container == containerId);
        }

        public static async Task PutObjectAsserts(this Client client, string containerId, string objectId, int maxBufferSize)
        {
            // generate random byte array
            RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
            var data = generator.GenerateBufferFromSeed(maxBufferSize);

            // upload
            var createRsp = await client.PutObjectAsync(containerId, objectId, data);

            Assert.True(createRsp.IsSuccess);

            // exists
            var existsRsp = await client.HeadObjectAsync(containerId, objectId);

            Assert.True(existsRsp.IsSuccess && existsRsp.ContentLength == maxBufferSize);

            // get
            var getRsp = await client.GetObjectAsync(containerId, objectId);

            Assert.True(getRsp.IsSuccess && getRsp.Stream != null);

            using var ms = new MemoryStream();
            await getRsp.Stream.CopyToAsync(ms);

            Assert.True(ms.Length == maxBufferSize);
        }

        public static async Task PutPseudoDirectoryAsserts(this Client client, string containerId, string pseudoDirectoryId)
        {
            var createRsp = await client.PutPseudoDirectoryAsync(containerId, pseudoDirectoryId);

            Assert.True(createRsp.IsSuccess);

            using var getRsp = await client.GetObjectAsync(containerId, pseudoDirectoryId);

            Assert.True(getRsp.IsSuccess);
        }

        public static async Task PutChunkedObjectAsserts(this Client client, string containerId, string chunkedObjectId, int chunks, int maxBufferSize)
        {
            var tasks = new List<Task>();

            // upload chunks
            for (var i = 0; i < chunks; i++)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(maxBufferSize);
                var data = generator.GenerateBufferFromSeed(maxBufferSize);

                var task = client.PutObjectChunkAsync(containerId, chunkedObjectId, data, i);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // upload manifest
            var manifestResp = await client.PutManifestAsync(containerId, chunkedObjectId);

            Assert.True(manifestResp.IsSuccess);

            // exists
            var existsRsp = await client.HeadObjectAsync(containerId, chunkedObjectId);

            Assert.True(existsRsp.IsSuccess && existsRsp.ContentLength == maxBufferSize * chunks);

            // get object
            using var getRsp = await client.GetObjectAsync(containerId, chunkedObjectId);

            Assert.True(getRsp.IsSuccess && getRsp.Stream != null);

            using var ms1 = new MemoryStream();

            await getRsp.Stream.CopyToAsync(ms1);

            Assert.True(ms1.Length == maxBufferSize * chunks);

            // get chunk
            using var chunkResp = await client.GetObjectRangeAsync(containerId, chunkedObjectId, 0, maxBufferSize - 1);

            Assert.True(chunkResp.IsSuccess && chunkResp.Stream != null);

            using var ms = new MemoryStream();

            await chunkResp.Stream.CopyToAsync(ms);

            Assert.True(ms.Length == maxBufferSize);
        }
    }
}

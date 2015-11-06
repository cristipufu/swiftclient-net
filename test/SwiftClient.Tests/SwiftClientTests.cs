using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

using SwiftClient.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SwiftClient.Tests
{
    [TestClass()]
    public class SwiftClientTests
    {
        private static ISwiftClient _client;

        private string _containerId = "testcontainer";
        private string _objectId = "testfile";
        private string _chunkedObjectId = "testfilechunks";
        private int _maxBufferSize = 10 * 1024;

        public SwiftClientTests() { }

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            if (_client == null)
            {
                _client = new SwiftClient()
                    .WithCredentials(new SwiftCredentials
                    {
                        Username = "preview:root",
                        Password = "swift@VT!@#",
                        Endpoints = new List<string> { "http://192.168.3.21:8080" }
                    })
                    .SetRetryCount(2)
                    .SetLogger(new SwiftLogger());
            }
        }

        [TestMethod()]
        public async Task AuthenticateTest()
        {
            // auth
            var rsp = await _client.Authenticate();

            Assert.IsTrue(rsp != null && !string.IsNullOrEmpty(rsp.AuthToken));
        }

        [TestMethod()]
        public async Task PutContainerTest()
        {
            // create
            var createRsp = await _client.PutContainer(_containerId);

            Assert.IsTrue(createRsp.IsSuccess);

            // exists
            var existsRsp = await _client.HeadContainer(_containerId);

            Assert.IsTrue(existsRsp.IsSuccess);
        }

        [TestMethod()]
        public async Task PutObjectTest()
        {
            // generate random byte array
            RandomBufferGenerator generator = new RandomBufferGenerator(_maxBufferSize);
            var data = generator.GenerateBufferFromSeed(_maxBufferSize);

            // upload
            var createRsp = await _client.PutObject(_containerId, _objectId, data);

            Assert.IsTrue(createRsp.IsSuccess);

            // exists
            var existsRsp = await _client.HeadObject(_containerId, _objectId);

            Assert.IsTrue(existsRsp.IsSuccess && existsRsp.ContentLength == _maxBufferSize);

            // get
            var getRsp = await _client.GetObject(_containerId, _objectId);

            Assert.IsTrue(getRsp.IsSuccess && getRsp.Stream != null && getRsp.Stream.Length == _maxBufferSize);
        }

        [TestMethod()]
        public async Task PutChunkedObjectTest()
        {
            var chunks = 10;

            // upload chunks
            for (var i = 0; i < chunks; i++)
            {
                // generate random byte array
                RandomBufferGenerator generator = new RandomBufferGenerator(_maxBufferSize);
                var data = generator.GenerateBufferFromSeed(_maxBufferSize);

                // upload
                var createRsp = await _client.PutChunkedObject(_containerId, _chunkedObjectId, data, i);

                Assert.IsTrue(createRsp.IsSuccess);
            }

            // upload manifest
            var manifestResp = await _client.PutManifest(_containerId, _chunkedObjectId);

            Assert.IsTrue(manifestResp.IsSuccess);

            // exists
            var existsRsp = await _client.HeadObject(_containerId, _chunkedObjectId);

            Assert.IsTrue(existsRsp.IsSuccess && existsRsp.ContentLength == _maxBufferSize * chunks);

            // get object
            var getRsp = await _client.GetObject(_containerId, _chunkedObjectId);

            Assert.IsTrue(getRsp.IsSuccess && getRsp.Stream != null && getRsp.Stream.Length == _maxBufferSize * chunks);

            // get chunk
            var chunkResp = await _client.GetObjectRange(_containerId, _chunkedObjectId, 0, _maxBufferSize - 1);

            Assert.IsTrue(chunkResp.IsSuccess && chunkResp.Stream != null && chunkResp.Stream.Length == _maxBufferSize);
        }

        [TestMethod()]
        public async Task GetAccountTest()
        {
            var resp = await _client.GetAccount(new Dictionary<string, string>() { { "format", "json" } });

            Assert.IsTrue(resp.IsSuccess);

            var containersList = JsonConvert.DeserializeObject<List<ContainerInfoModel>>(resp.Info);

            Assert.IsTrue(containersList.Any(x => x.name == _containerId));
        }

        [TestMethod()]
        public async Task GetContainerTest()
        {
            var resp = await _client.GetContainer(_containerId, null, new Dictionary<string, string>() { { "format", "json" } });

            Assert.IsTrue(resp.IsSuccess);

            var objectsList = JsonConvert.DeserializeObject<List<ObjectInfoModel>>(resp.Info);

            Assert.IsTrue(objectsList.Count > 0 && objectsList[0].bytes == _maxBufferSize);
        }
    }
}
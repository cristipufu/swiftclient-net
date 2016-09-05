using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Test
{
    public class SwiftFixture
    {
        public SwiftCredentials Credentials;
        public string ContainerId = "testcontainer";
        public string PseudoDirectoryId = "pseudodirectory";
        public string ObjectId = "testfile";
        public string ChunkedObjectId = "testfilechunks";
        public int MaxBufferSize = 10 * 1024;
        public int Chunks = 10;

        public SwiftFixture()
        {
            Credentials = GetConfigCredentials();
        }

        public virtual void Dispose()
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
}

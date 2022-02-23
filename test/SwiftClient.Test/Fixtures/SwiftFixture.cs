using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient.Test
{
    public class SwiftFixture
    {
        public SwiftCredentials Credentials;
        public IHttpClientFactory HttpClientFactory;

        public string ContainerId = "testcontainer";
        public string PseudoDirectoryId = "pseudodirectory";
        public string ObjectId = "testfile";
        public string ChunkedObjectId = "testfilechunks";
        public string VideoPathFormat = "/home/playvideo?containerId={0}&objectId={1}";
        public string VideoId = "videoFile.mp4";

        public int MaxBufferSize = 10 * 1024;
        public int Chunks = 10;

        public SwiftFixture()
        {
            Credentials = GetConfigCredentials();
            HttpClientFactory = CreateHttpClientFactory();
        }

        public virtual void Dispose()
        {
            using var client = new Client(Credentials);

            var deleteFilesTasks = new List<Task>();

            deleteFilesTasks.Add(client.DeleteObjectAsync(ContainerId, ObjectId));
            deleteFilesTasks.Add(client.DeleteObjectAsync(ContainerId, ChunkedObjectId));

            for (var i = 0; i < Chunks; i++)
            {
                deleteFilesTasks.Add(client.DeleteObjectChunkAsync(ContainerId, ChunkedObjectId, i));
            }

            var delContainerTask = client.DeleteContainerAsync(ContainerId);

            Task.WhenAll(deleteFilesTasks)
            .ContinueWith((rsp) => delContainerTask)
            .Wait();
        }

        private static SwiftCredentials GetConfigCredentials()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            IConfiguration configuration = builder.Build();
            var section = configuration.GetSection("SwiftCluster");
            return section.Get<SwiftCredentials>();
        }

        private static IHttpClientFactory CreateHttpClientFactory()
        {
            var services = new ServiceCollection();

            services.AddHttpClient("swift", (client) =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            });

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IHttpClientFactory>();
        }
    }
}

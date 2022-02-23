using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net.Http;

namespace SwiftClient.Test
{
    public class SwiftFixtureWebHost<TStartup> : SwiftFixture
        where TStartup : class
    {
        private readonly IHost _server;

        public SwiftFixtureWebHost(string baseUri) : base()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.UseStartup<TStartup>();
                    ConfigureWebHost(webHost);
                });

            _server = hostBuilder.Start();

            HttpClient = _server.GetTestClient();
            HttpClient.BaseAddress = new Uri(baseUri);
        }

        private void ConfigureWebHost(IWebHostBuilder builder)
        {
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "appsettings.json");

            IConfigurationRoot configurationRoot = null;
            builder.ConfigureAppConfiguration((context, conf) =>
            {
                conf.AddJsonFile(configPath);
                configurationRoot = conf.Build();
            });
        }

        public HttpClient HttpClient { get; }

        public override void Dispose()
        {
            HttpClient.Dispose();
            _server.Dispose();

            base.Dispose();
        }
    }
}

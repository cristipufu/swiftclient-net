using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient.Test
{
    public class SwiftFixtureWebHost<TStartup> : SwiftFixture
        where TStartup : class
    {
        private readonly TestServer _server;

        public SwiftFixtureWebHost(string baseUri) : base()
        {
            var builder = new WebHostBuilder().UseStartup<TStartup>();
            _server = new TestServer(builder);

            HttpClient = _server.CreateClient();
            HttpClient.BaseAddress = new Uri(baseUri);
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

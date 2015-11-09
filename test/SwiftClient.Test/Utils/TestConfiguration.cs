using SwiftClient.Models;
using Xunit;

namespace SwiftClient.Test
{
    public class TestConfiguration : TestAssemblyConfiguration
    {
        public SwiftCredentials Credentials { get; set; }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Test
{
    public class SwiftFixtureDemo<TStartup> : SwiftFixtureWebHost<TStartup>
        where TStartup : class
    {
        public SwiftFixtureDemo() : base("http://localhost:5000")
        {
        }
    }
}

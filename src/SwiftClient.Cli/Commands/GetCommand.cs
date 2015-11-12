using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public static class GetCommand
    {
        public static int Run(GetOptions options, SwiftClient client)
        {
            Console.WriteLine($"Download {options.Object} to {options.File} ");
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("rm", HelpText = "remove container and it's content or a single object if one is specified")]
    public class DeleteOptions
    {
        [Option('c', "container", Required = true, HelpText = "container")]
        public string Container { get; set; }

        [Option('o', "object", Required = false, HelpText = "object")]
        public string Object { get; set; }

        [Option('l', "limit", Required = false, Default = 1000, HelpText = "number of objects to delete in one call")]
        public int Limit { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("ls", HelpText = "list containers or objects in container if a container is specified")]
    public class ListOptions
    {
        [Option('c', "container", Required = false, HelpText = "container name")]
        public string Container { get; set; }

        [Option('d', "delimiter", Required = false, HelpText = "delimiter")]
        public string Delimiter { get; set; }

        [Option('p', "prefix", Required = false, HelpText = "prefix")]
        public string Prefix { get; set; }
    }
}

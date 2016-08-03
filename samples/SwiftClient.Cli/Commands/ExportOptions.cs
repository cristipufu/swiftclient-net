using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("export", HelpText = "export all objects for account or objects in container if a container is specified")]
    public class ExportOptions
    {
        [Option('c', "container", Required = false, HelpText = "container name")]
        public string Container { get; set; }

        [Option("prefix", Required = false, HelpText = "prefix")]
        public string Prefix { get; set; }

        [Option('p', "path", Required = false, HelpText = "path")]
        public string Path { get; set; }
    }
}

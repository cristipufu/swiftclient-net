using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("import", HelpText = "import containers with objects to account")]
    public class ImportOptions
    {
        [Option('c', "container", Required = false, HelpText = "container name")]
        public string Container { get; set; }

        [Option('p', "path", Required = false, HelpText = "path")]
        public string Path { get; set; }
    }
}

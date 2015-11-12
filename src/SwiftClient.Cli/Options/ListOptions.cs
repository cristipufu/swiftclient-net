using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("ls", HelpText = "list objects in container")]
    public class ListOptions
    {
        [Option('c', "container", Required = true, HelpText = "container name")]
        public string Object { get; set; }
    }
}

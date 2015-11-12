using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("get", HelpText = "download file")]
    public class GetOptions
    {
        [Option('o', "object", Required = true, HelpText = "container/object")]
        public string Object { get; set; }

        [Option('f', "file", Required = true, HelpText = "destination file path")]
        public string File { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("put", HelpText = "upload file")]
    public class PutOptions
    {
        [Option('c', "container", Required = true, HelpText = "swift container id")]
        public string Container { get; set; }

        [Option('o', "object", Required = true, HelpText = "swift object id")]
        public string Object { get; set; }

        [Option('f', "file", Required = true, HelpText = "input file to be uploaded")]
        public string File { get; set; }
    }
}

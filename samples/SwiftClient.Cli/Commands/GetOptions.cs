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
        [Option('c', "container", Required = true, HelpText = "container")]
        public string Container { get; set; }

        [Option('o', "object", Required = true, HelpText = "object")]
        public string Object { get; set; }

        [Option('f', "file", Required = true, HelpText = "destination file path")]
        public string File { get; set; }

        [Option('b', "buffer", Required = false, Default = 2, HelpText = "buffer size in MB, default is 2MB")]
        public int BufferSize { get; set; }
    }
}

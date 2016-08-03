using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace SwiftClient.Cli
{
    [Verb("login", HelpText = "swift login")]
    public class LoginOptions
    {
        [Option('h', "host", Required = true, HelpText = "swift proxy URL")]
        public string Endpoint { get; set; }

        [Option('u', "user", Required = true, HelpText = "username")]
        public string User { get; set; }

        [Option('p', "password", Required = true, HelpText = "password")]
        public string Password { get; set; }
    }
}

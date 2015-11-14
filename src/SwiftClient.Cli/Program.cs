using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using Humanizer;
using Humanizer.Bytes;

namespace SwiftClient.Cli
{
    public class Program
    {
        Client client = null;

        public async Task Main(string[] args)
        {
            client = await new AuthManager(args).Connect();

            var command = Console.ReadLine();

            while (command != "exit")
            {
                try
                {
                    var exitCode = CommandLine.Parser.Default.ParseArguments<
                        StatsOptions,
                        PutOptions, 
                        GetOptions, 
                        ListOptions, 
                        DeleteOptions>(command.ParseArguments()).MapResult(
                        (StatsOptions opts) => StatsCommand.Run(opts, client),
                        (PutOptions opts) => PutCommand.Run(opts, client),
                        (GetOptions opts) => GetCommand.Run(opts, client),
                        (ListOptions opts) => ListCommand.Run(opts, client),
                        (DeleteOptions opts) => DeleteCommand.Run(opts, client),
                        errs => 1);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message);       
                }

                command = Console.ReadLine();
            }
            
        }
    }
}

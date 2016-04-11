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
            var authManager = new AuthManager(args);

            client = await authManager.Connect();

            var command = Console.ReadLine();

            while (command != "exit")
            {
                try
                {
                    var exitCode = Parser.Default.ParseArguments<
                        StatsOptions,
                        PutOptions, 
                        GetOptions, 
                        ListOptions, 
                        ExportOptions,
                        ImportOptions,
                        DeleteOptions>(command.ParseArguments()).MapResult(
                        (StatsOptions opts) => StatsCommand.Run(opts, client),
                        (PutOptions opts) => PutCommand.Run(opts, client),
                        (GetOptions opts) => GetCommand.Run(opts, client),
                        (ListOptions opts) => ListCommand.Run(opts, client),
                        (ExportOptions opts) => ExportCommand.Run(opts, client, authManager),
                        (ImportOptions opts) => ImportCommand.Run(opts, client, authManager),
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

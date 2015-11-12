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
        SwiftClient client = null;
        SwiftCredentials credentials = new SwiftCredentials();

        public async Task Main(string[] args)
        {
            var isConnected = await Connect();

            var command = Console.ReadLine();

            while (command != "exit")
            {
                
                var exitCode = CommandLine.Parser.Default.ParseArguments<PutOptions, GetOptions, ListOptions>(command.ParseArguments()).MapResult(
                    (PutOptions opts) => PutCommand.Run(opts, client),
                    (GetOptions opts) => GetCommand.Run(opts, client),
                    (ListOptions opts) => ListCommand.Run(opts, client),
                    errs => 1);

                command = Console.ReadLine();
            }
            
        }

        private async Task<bool> Connect()
        {
            var needsAuth = !ValidateLogin();
            if (needsAuth)
            {
                while (needsAuth)
                {
                    needsAuth = !PromptLogin();
                    needsAuth = !await DoLogin();
                }
            }
            else
            {
                needsAuth = !await DoLogin();
                while (needsAuth)
                {
                    needsAuth = !PromptLogin();
                    needsAuth = !await DoLogin();
                }
            }

            return needsAuth;
        }

        private bool ValidateLogin()
        {
            bool exists = true;

            var endpoint = Environment.GetEnvironmentVariable("SWIFT_URL");
            var username = Environment.GetEnvironmentVariable("SWIFT_USER");
            var password = Environment.GetEnvironmentVariable("SWIFT_KEY");

            if (string.IsNullOrEmpty(endpoint))
            {
                Console.WriteLine("SWIFT_URL environment variable not set");
                exists = false;
            }
            else
            {
                credentials.Endpoints = new List<string> { endpoint };
            }

            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("SWIFT_USER environment variable not set");
                exists = false;
            }
            else
            {
                credentials.Username = username;
            }

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("SWIFT_KEY environment variable not set");
                exists = false;
            }
            else
            {
                credentials.Password = password;
            }

            return exists;
        }

        private bool PromptLogin()
        {
            Console.WriteLine($"Connect to swift using command: login -h http://localhost:8080 -u username -p password");
            var loginCommand = Console.ReadLine();

            var exitCode = CommandLine.Parser.Default.ParseArguments<LoginOptions>(loginCommand.ParseArguments()).MapResult(
                options => {
                    credentials.Endpoints = new List<string> { options.Endpoint };
                    credentials.Username = options.User;
                    credentials.Password = options.Password;
                    return 0;
                },
                errs => 1);

            return exitCode == 0;
        }

        private async Task<bool> DoLogin()
        {
            bool ok = false;

            Console.WriteLine($"Connecting to {credentials.Endpoints.FirstOrDefault()} as {credentials.Username}");

            client = new SwiftClient(new SwiftAuthManager(credentials))
                .SetRetryCount(1)
                .SetLogger(new SwiftLogger());

            var data = await client.Authenticate();

            if (data != null)
            {
                ok = true;
                Console.WriteLine($"Connected to {data.StorageUrl}");
            }

            return ok;
        }
    }
}

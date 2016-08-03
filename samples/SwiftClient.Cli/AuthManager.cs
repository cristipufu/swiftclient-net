using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public class AuthManager
    {
        bool validLoginData = false;
        Client client = null;
        SwiftCredentials credentials = new SwiftCredentials();

        public AuthManager(string[] args)
        {
            if(args.Length > 0 && ParseLoginCommand(args) == 0)
            {
                validLoginData = true;
            }
        }

        public async Task<Client> Connect()
        {
            var needsAuth = validLoginData ? false : !ValidateLogin();
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

            return client;
        }

        public SwiftCredentials Credentials()
        {
            return credentials;
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
            Console.WriteLine($"Connect to swift using command:");
            Console.WriteLine("login -h <host> -u <user> -p <password>");
            var loginCommand = Console.ReadLine();

            var exitCode = ParseLoginCommand(loginCommand.ParseArguments());

            return exitCode == 0;
        }

        private int ParseLoginCommand(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<LoginOptions>(args).MapResult(
                    options => {
                        credentials.Endpoints = new List<string> { options.Endpoint };
                        credentials.Username = options.User;
                        credentials.Password = options.Password;
                        return 0;
                    },
                    errs => 1);
        }

        private async Task<bool> DoLogin()
        {
            bool ok = false;

            Console.WriteLine($"Connecting to {credentials.Endpoints.FirstOrDefault()} as {credentials.Username}");

            client = new Client(new SwiftAuthManager(credentials))
                .SetRetryCount(1)
                .SetLogger(new SwiftConsoleLog());

            var data = await client.Authenticate();

            if (data != null)
            {
                ok = true;
                Console.WriteLine($"Authentication token received from {data.StorageUrl}");
            }

            return ok;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;

namespace SwiftClient.Cli
{
    public class Program
    {
        SwiftClient client = null;
        SwiftCredentials credentials = new SwiftCredentials();
        long bufferSize = 1000000;

        public async Task Main(string[] args)
        {
            var endpoint = Environment.GetEnvironmentVariable("SWIFT_URL");
            var username = Environment.GetEnvironmentVariable("SWIFT_USER");
            var password = Environment.GetEnvironmentVariable("SWIFT_KEY");

            var needsAuth = false;

            if(string.IsNullOrEmpty(endpoint))
            {
                Console.WriteLine("SWIFT_URL environment variable not set");
                needsAuth = true;
            }
            else
            {
                credentials.Endpoints = new List<string> { endpoint };
            }

            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("SWIFT_USER environment variable not set");
                needsAuth = true;
            }
            else
            {
                credentials.Username = username;
            }

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("SWIFT_KEY environment variable not set");
                needsAuth = true;
            }
            else
            {
                credentials.Password = password;
            }

            if (needsAuth)
            {
                Console.WriteLine($"Connect to swift using command: login -h http://localhost:8080 -u username -p password");
                var loginCommand = Console.ReadLine();

                var exitCode = CommandLine.Parser.Default.ParseArguments<LoginOptions>(loginCommand.ParseArguments()).MapResult(
                    options => {
                        endpoint = options.Endpoint;
                        username = options.User;
                        password = options.Password;
                        credentials.Endpoints = new List<string> { options.Endpoint };
                        credentials.Username = options.User;
                        credentials.Password = options.Password;
                        return 0;
                    },
                    errs => 1);

               if (exitCode != 0) return;
            }

            Console.WriteLine($"Connecting to {endpoint} as {username}");

            client = new SwiftClient(new SwiftAuthManager(credentials))
                .SetRetryCount(2)
                .SetLogger(new SwiftLogger());

            var data = await client.Authenticate();

            if (data != null)
            {
                Console.WriteLine($"Connected to {data.StorageUrl}");
            }

            var command = Console.ReadLine();
            var regex = new Regex(@"\w+|""[\w\s]*""");
            while (command != "exit")
            {
                
                var exitCode = CommandLine.Parser.Default.ParseArguments<PutOptions, GetOptions, ListOptions>(command.ParseArguments()).MapResult(
                    (PutOptions opts) => RunPut(opts),
                    (GetOptions opts) => RunGet(opts),
                    (ListOptions opts) => RunList(opts),
                    errs => 1);

                command = Console.ReadLine();
            }
            
        }

        private int RunPut(PutOptions options)
        {
            options.File = options.File.Replace('"', ' ').Trim();
            if (!File.Exists(options.File))
            {
                Console.WriteLine($"File not found {options.File}");
                return 404;
            }

            Console.WriteLine($"Uploading {options.File} to {options.Object}");

            var response = new SwiftResponse();
            var fileName = Path.GetFileNameWithoutExtension(options.File);
            string containerTemp = options.Container + "_tmp";
            byte[] buffer = new byte[bufferSize];

            response = client.PutContainer(containerTemp).Result;

            if (!response.IsSuccess)
            {
                Console.WriteLine($"Put container error {response.Reason}");
                return 500;
            }

            response = client.PutContainer(options.Container).Result;

            if (!response.IsSuccess)
            {
                Console.WriteLine($"Put container error {response.Reason}");
                return 500;
            }

            using (var stream = File.OpenRead(options.File))
            {
                int chunks = 0;
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    using (MemoryStream tmpStream = new MemoryStream())
                    {
                        tmpStream.Write(buffer, 0, bytesRead);

                        response = client.PutChunkedObject(containerTemp, fileName, tmpStream.ToArray(), chunks).Result;
                        if (!response.IsSuccess)
                        {
                            Console.WriteLine($"Uploading error {response.Reason}");
                            return 500;
                        }
                    }
                    chunks++;
                }

                // use manifest to merge chunks
                response = client.PutManifest(containerTemp, fileName).Result;

                if (!response.IsSuccess)
                {
                    Console.WriteLine($"Put manifest error {response.Reason}");
                    return 500;
                }

                // copy chunks to new file and set some meta data info about the file (filename, contentype)
                response = client.CopyObject(containerTemp, fileName, options.Container, options.Object, new Dictionary<string, string>
                {
                    { string.Format(SwiftHeaderKeys.ObjectMetaFormat, "Filename"), fileName },
                    { string.Format(SwiftHeaderKeys.ObjectMetaFormat, "Contenttype"), Path.GetExtension(options.File) }
                }).Result;

                if (!response.IsSuccess)
                {
                    Console.WriteLine($"Copy object error {response.Reason}");
                    return 500;
                }

                // cleanup temp chunks
                var deleteTasks = new List<Task<SwiftResponse>>();

                for (var i = 0; i < chunks; i++)
                {
                    deleteTasks.Add(client.DeleteObjectChunk(containerTemp, fileName, i));
                }

                // cleanup manifest
                deleteTasks.Add(client.DeleteObject(containerTemp, fileName));

                // cleanup temp container
                Task.WhenAll(deleteTasks).ContinueWith((rsp) =>
                {
                    response = rsp.Result.FirstOrDefault(x => !x.IsSuccess);

                    if (response != null)
                    {
                        Console.WriteLine($"Cleanup temp chunks error {response.Reason}");
                    }

                    if (rsp.Result.All(x => x.IsSuccess))
                    {
                        response = client.DeleteContainer(containerTemp).Result;
                    }

                }).Wait();

                if (!response.IsSuccess)
                {
                    Console.WriteLine($"Cleanup error {response.Reason}");
                    return 500;
                }

                Console.WriteLine($"Upload done");
            }

            return 0;
        }

        private int RunGet(GetOptions options)
        {
            Console.WriteLine($"Download {options.Object} to {options.File} ");
            return 0;
        }

        private int RunList(ListOptions options)
        {
            var containerData = client.GetContainer(options.Object, null, new Dictionary<string, string> { { "format", "json" } }).Result;
            if (containerData.IsSuccess)
            {
                if (!string.IsNullOrEmpty(containerData.Info))
                {
                    var viewModel = new ContainerViewModel();
                    viewModel.Objects = JsonConvert.DeserializeObject<List<ObjectViewModel>>(containerData.Info);
                    var table = viewModel.Objects.ToStringTable(
                        u => u.name,
                        u => u.hash,
                        u => u.content_type,
                        u => u.bytes,
                        u => u.last_modified
                    );
                    Console.WriteLine(table);
                }
            }
            else
            {
                Console.WriteLine(containerData.Reason);
            }
            return 0;
        }


    }
}

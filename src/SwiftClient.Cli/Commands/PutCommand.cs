using Humanizer.Bytes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;

namespace SwiftClient.Cli
{
    public static class PutCommand
    {
        static long bufferSize = Convert.ToInt64(ByteSize.FromMegabytes(2).Bytes);

        public static int Run(PutOptions options, SwiftClient client)
        {
            var stopwatch = Stopwatch.StartNew();
            options.File = options.File.Replace('"', ' ').Trim();
            if (!File.Exists(options.File))
            {
                Logger.LogError($"File not found {options.File}");
                return 404;
            }

            Logger.Log($"Uploading {options.File} to {options.Object}");

            var response = new SwiftBaseResponse();
            var fileName = Path.GetFileNameWithoutExtension(options.File);
            string containerTemp = options.Container + "_tmp";
            byte[] buffer = new byte[bufferSize];

            response = client.PutContainer(containerTemp).Result;

            if (!response.IsSuccess)
            {
                Logger.LogError($"Put temporary container error {response.Reason}");
                return 500;
            }

            response = client.PutContainer(options.Container).Result;

            if (!response.IsSuccess)
            {
                Logger.LogError($"Put container error {response.Reason}");
                return 500;
            }

            using (var stream = File.OpenRead(options.File))
            {
                Console.Write($"\rUploading...");

                int chunks = 0;
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    using (MemoryStream tmpStream = new MemoryStream())
                    {
                        tmpStream.Write(buffer, 0, bytesRead);
                        var data = tmpStream.ToArray();
                        response = client.PutChunkedObject(containerTemp, fileName, data, chunks).Result;

                        Console.Write($"\rUploaded {((chunks * 2).Megabytes() + data.LongLength.Bytes()).Humanize("MB")}");

                        if (!response.IsSuccess)
                        {
                            Logger.LogError($"Uploading error {response.Reason}");
                            return 500;
                        }
                    }
                    chunks++;
                }

                Console.Write("\rMerging chunks... ");

                // use manifest to merge chunks
                response = client.PutManifest(containerTemp, fileName).Result;

                if (!response.IsSuccess)
                {
                    Logger.LogError($"Put manifest error {response.Reason}");
                    return 500;
                }

                // copy chunks to new file and set some meta data info about the file (filename, contentype)
                response = client.CopyObject(containerTemp, fileName, options.Container, options.Object, new Dictionary<string, string>
                {
                    { "X-Object-Meta-Filename", fileName },
                    { "X-Object-Meta-Contenttype", Path.GetExtension(options.File) }
                }).Result;

                if (!response.IsSuccess)
                {
                    Logger.LogError($"Copy object error {response.Reason}");
                    return 500;
                }

                // cleanup temp
                response = client.DeleteContainerWithContents(containerTemp).Result;

                if (!response.IsSuccess)
                {
                    Logger.LogError($"Cleanup error {response.Reason}");
                    return 500;
                }

                Console.Write($"\rUpload done in {stopwatch.ElapsedMilliseconds.Milliseconds().Humanize()}");
                Console.Write(Environment.NewLine);
            }

            return 0;
        }
    }
}

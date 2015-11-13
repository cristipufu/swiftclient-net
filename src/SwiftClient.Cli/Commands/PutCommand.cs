using Humanizer.Bytes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using System.Threading;

namespace SwiftClient.Cli
{
    public static class PutCommand
    {
        public static int Run(PutOptions options, SwiftClient client)
        {
            if (!string.IsNullOrEmpty(options.File))
            {
                options.File = options.File.Replace('"', ' ').Trim();
            }
            else
            {
                Logger.LogError($"Not found {options.File}");
                return 404;
            }

            if (!string.IsNullOrEmpty(options.Object) || !Directory.Exists(options.File))
            {
                return UploadFile(options, client);
            }
            else
            {
                return UploadDirectory(options, client);
            }
        }
        public static int UploadFile(PutOptions options, SwiftClient client, bool showProgress = true)
        {
            var stopwatch = Stopwatch.StartNew();
            long bufferSize = Convert.ToInt64(ByteSize.FromMegabytes(options.BufferSize).Bytes);
            
            if (!File.Exists(options.File))
            {
                Logger.LogError($"File not found {options.File}");
                return 404;
            }

            if (showProgress) Logger.Log($"Uploading {options.File} to {options.Object}");

            var response = new SwiftBaseResponse();
            var fileName = Path.GetFileNameWithoutExtension(options.File);
            string containerTemp = "tmp_" + Guid.NewGuid().ToString("N");
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
                if(showProgress) Console.Write($"\rUploading...");

                int chunks = 0;
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    using (MemoryStream tmpStream = new MemoryStream())
                    {
                        tmpStream.Write(buffer, 0, bytesRead);
                        var data = tmpStream.ToArray();
                        response = client.PutChunkedObject(containerTemp, fileName, data, chunks).Result;

                        if (showProgress) Console.Write($"\rUploaded {((chunks * options.BufferSize).Megabytes() + data.LongLength.Bytes()).Humanize("MB")}");

                        if (!response.IsSuccess)
                        {
                            Logger.LogError($"Uploading error {response.Reason}");
                            return 500;
                        }
                    }
                    chunks++;
                }

                if (showProgress) Console.Write("\rMerging chunks... ");

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
                    //TODO: determine mime type 
                    //{ "X-Object-Meta-Contenttype", Path.GetExtension(options.File) }
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

                if (showProgress) Console.Write($"\rUpload done in {stopwatch.ElapsedMilliseconds.Milliseconds().Humanize()}");
                if (showProgress) Console.Write(Environment.NewLine);
            }

            return 0;
        }

        public static int UploadDirectory(PutOptions options, SwiftClient client)
        {
            if(!Directory.Exists(options.File))
            {
                Logger.LogError($"Directory not found {options.File}");
                return 404;
            }

            var files = Directory.GetFiles(options.File, "*", SearchOption.AllDirectories);

            int total = files.Length;
            int done = 0;

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
            Parallel.ForEach(files, parallelOptions, file =>
            {
                //put sub-directory path in object name
                var objectName = file.Replace(options.File, "");
                if (objectName.StartsWith("\\") || objectName.StartsWith("/"))
                {
                    objectName = objectName.Substring(1, objectName.Length - 1);
                }

                var meta = new PutOptions
                {
                    BufferSize = options.BufferSize,
                    Container = options.Container,
                    File = file,
                    Object = objectName
                };

                UploadFile(meta, client, false);
                Interlocked.Increment(ref done);
                Console.Write($"\rUploaded {done}/{total}");
            });

            Logger.Log("Files uploaded");
            return 0;
        }



    }
}

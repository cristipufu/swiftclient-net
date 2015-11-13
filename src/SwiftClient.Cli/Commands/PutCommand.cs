using Humanizer.Bytes;
using System;
using System.Diagnostics;
using System.IO;
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

            response = client.PutContainer(options.Container).Result;

            if (!response.IsSuccess)
            {
                Logger.LogError($"Put container error {response.Reason}");
                return 500;
            }

            using (var stream = File.OpenRead(options.File))
            {
                if (showProgress)
                    Console.Write($"\rUploading...");

                response = client.PutLargeObject(options.Container, options.Object, stream, (chunk, bytesRead) =>
                {
                    if (showProgress)
                        Console.Write($"\rUploaded {((chunk * options.BufferSize).Megabytes() + bytesRead.Bytes()).Humanize("MB")}");

                }, Convert.ToInt64(ByteSize.FromMegabytes(options.BufferSize).Bytes)).Result;

                if (!response.IsSuccess)
                {
                    Logger.LogError($"Uploading error {response.Reason}");
                    return 500;
                }

                if (showProgress) Console.Write($"\rUpload done in {stopwatch.ElapsedMilliseconds.Milliseconds().Humanize()}");
                if (showProgress) Console.Write(Environment.NewLine);
            }

            return 0;
        }

        public static int UploadDirectory(PutOptions options, SwiftClient client)
        {
            var stopwatch = Stopwatch.StartNew();

            if (!Directory.Exists(options.File))
            {
                Logger.LogError($"Directory not found {options.File}");
                return 404;
            }

            var files = Directory.GetFiles(options.File, "*", SearchOption.AllDirectories);

            int total = files.Length;
            int done = 0;

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = options.Parallel;
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

            Console.Write($"\rUpload done in {stopwatch.ElapsedMilliseconds.Milliseconds().Humanize()}");
            return 0;
        }



    }
}

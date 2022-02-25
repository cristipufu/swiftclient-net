using Humanizer.Bytes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Humanizer;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace SwiftClient.Cli
{
    public static class ImportCommand
    {
        public class Importer
        {
            public class UploadObject
            {
                public string Container { get; set; }

                public string Object { get; set; }

                public string Path { get; set; }
            }

            public class UploadResponse
            {
                public bool IsSuccess { get; set; }
                public string Message { get; set; }
            }

            public class FailedObject
            {
                public string Message { get; set; }

                public string Container { get; set; }

                public UploadObject Object { get; set; }
            }

            long counter = 0;
            string rootPath;
            List<UploadObject> uploadBag = new List<UploadObject>();
            ConcurrentQueue<FailedObject> failedQueue = new ConcurrentQueue<FailedObject>();
            SwiftCredentials credentials;

            public Importer(SwiftCredentials credentials, string rootPath)
            {
                this.credentials = credentials;
                this.rootPath = rootPath;
            }

            public void PutObjects()
            {
                var dirs = Directory.GetDirectories(rootPath);

                foreach(var dir in dirs)
                {
                    var files = Directory.GetFiles(dir);

                    foreach(var file in files)
                    {
                        uploadBag.Add(new UploadObject
                        {
                            Container = new DirectoryInfo(dir).Name,
                            Object = PathEscaper.Unescape(Path.GetFileName(file)),
                            Path = file
                        });
                    }
                }

                if (uploadBag.Any())
                {
                    Console.Write("Importing... ");

                    var objCount = uploadBag.Count;

                    var progress = new ProgressBar();

                    Parallel.For(0, objCount - 1, new ParallelOptions { MaxDegreeOfParallelism = 10 },
                      i =>
                      {
                          var uploadObj = uploadBag.ElementAt(i);

                          try
                          {
                              var response = ImportObject(uploadObj).Result;

                              Interlocked.Increment(ref counter);

                              if (!response.IsSuccess)
                              {
                                  ManageFailed(new FailedObject
                                  {
                                      Container = uploadObj.Container,
                                      Message = response.Message,
                                      Object = uploadObj
                                  });
                              }
                          }
                          catch (Exception ex)
                          {
                              ManageFailed(new FailedObject
                              {
                                  Container = uploadObj.Container,
                                  Message = ex.Message,
                                  Object = uploadObj
                              });
                          }

                          progress.Report((double)counter / objCount);
                      });

                    progress.Report(1);
                    progress.Dispose();
                    Console.WriteLine(" Done.");
                }
            }

            private void ManageFailed(FailedObject obj)
            {
                failedQueue.Enqueue(obj);
            }

            private async Task<UploadResponse> ImportObject(UploadObject obj)
            {
                var uploadClient = new Client(new SwiftAuthManager(credentials))
                        .SetRetryCount(2)
                        .SetLogger(new SwiftConsoleLog());

                UploadResponse result = null;

                using (var stream = File.OpenRead(obj.Path))
                {
                    var response = await uploadClient.PutLargeObjectAsync(obj.Container, obj.Object, stream, null, null, Convert.ToInt64(ByteSize.FromMegabytes(10).Bytes));

                    if (response.IsSuccess)
                    {
                        result = new UploadResponse
                        {
                            IsSuccess = true
                        };
                    }
                    else
                    {
                        result = new UploadResponse
                        {
                            IsSuccess = false,
                            Message = response.Reason
                        };
                    }
                }

                return result;
            }
        }

        public static int Run(ImportOptions options, Client client, AuthManager authManager)
        {
            string importDir = options.Path ?? Directory.GetCurrentDirectory();

            var importer = new Importer(authManager.Credentials(), importDir);

            importer.PutObjects();

            return 0;   
        }
    }
}

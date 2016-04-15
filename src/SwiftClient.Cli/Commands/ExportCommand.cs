using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public static class ExportCommand
    {
        public class Exporter
        {
            public class DownloadResponse
            {
                public bool IsSuccess { get; set; }
                public string Message { get; set; }
            }

            public class FailedObject
            {
                public string Message { get; set; }

                public string Container { get; set; }

                public SwiftObjectModel Object { get; set; }
            }

            public class DownloadObject
            {
                public string Container { get; set; }

                public SwiftObjectModel Object { get; set; }

                public string FilePath { get; set; }
            }

            public class ContainerRequest
            {
                public string Container { get; set; }
                public Dictionary<string, string> Query { get; set; }
            }

            ConcurrentQueue<FailedObject> failedQueue = new ConcurrentQueue<FailedObject>();
            ConcurrentBag<DownloadObject> downloadBag = new ConcurrentBag<DownloadObject>();
            ConcurrentQueue<ContainerRequest> listQueue = new ConcurrentQueue<ContainerRequest>();

            int counter = 0;
            Client client;
            SwiftCredentials credentials;
            int bufferSize = 2000000;

            public Exporter(Client client, SwiftCredentials credentials)
            {
                this.client = client;
                this.credentials = credentials;
            }

            public void GetObjectsWithPrefix(string prefix, List<SwiftContainerModel> containers, string path)
            {
                var queryParams = new Dictionary<string, string>();
                queryParams.Add("prefix", prefix);

                ExportAll(containers, path, queryParams);
            }

            public void GetObjects(List<SwiftContainerModel> containers, string path)
            {
                ExportAll(containers, path);
            }

            private void ExportAll(List<SwiftContainerModel> containers, string path, Dictionary<string, string> queryParams = null)
            {
                foreach (var container in containers)
                {
                    listQueue.Enqueue(new ContainerRequest
                    {
                        Container = container.Container,
                        Query = queryParams
                    });
                }

                while (!listQueue.IsEmpty)
                {
                    ContainerRequest request = null;

                    if (listQueue.TryDequeue(out request))
                    {
                        var containerData = client.GetContainer(request.Container, null, request.Query).Result;
                        if (containerData.IsSuccess)
                        {
                            if (containerData.Objects.Count > 0)
                            {
                                if (containerData.Objects.Count < containerData.ObjectsCount)
                                {
                                    var marker = containerData.Objects.OrderByDescending(x => x.Object).Select(x => x.Object).FirstOrDefault();

                                    var newRequest = new ContainerRequest()
                                    {
                                        Container = request.Container,
                                        Query = request.Query
                                    };

                                    if (newRequest.Query == null)
                                    {
                                        newRequest.Query = new Dictionary<string, string>();
                                    }

                                    newRequest.Query["marker"] = marker;

                                    listQueue.Enqueue(newRequest);
                                }

                                var target = Path.Combine(path, request.Container);

                                if (!Directory.Exists(target))
                                {
                                    Directory.CreateDirectory(target);
                                }

                                EnqueueObjects(request.Container, containerData.Objects, target);
                            }
                            
                        }
                    }
                }

                if (downloadBag.Any())
                {
                    Console.Write("Exporting... ");

                    var objCount = downloadBag.Count;

                    var progress = new ProgressBar();

                    Parallel.For(0, objCount - 1, new ParallelOptions { MaxDegreeOfParallelism = 10 },
                      i =>
                      {
                          var downloadObj = downloadBag.ElementAt(i);

                          try
                          {
                              var response = ExportObject(downloadObj).Result;

                              Interlocked.Increment(ref counter);

                              if (!response.IsSuccess)
                              {
                                  ManageFailed(new FailedObject
                                  {
                                      Container = downloadObj.Container,
                                      Message = response.Message,
                                      Object = downloadObj.Object
                                  });
                              }
                          }
                          catch (Exception ex)
                          {
                              ManageFailed(new FailedObject
                              {
                                  Container = downloadObj.Container,
                                  Message = ex.Message,
                                  Object = downloadObj.Object
                              });
                          }

                          progress.Report((double)counter / objCount);
                      });

                    progress.Report(1);
                    progress.Dispose();
                    Console.WriteLine(" Done.");
                }

                if (failedQueue.Any())
                {
                    Console.WriteLine($"Failed objects: ");

                    var table = failedQueue.ToStringTable(
                            u => u.Container,
                            u => u.Object.Object,
                            u => u.Message
                        );
                    Console.WriteLine(table);
                }
            }

            private void ManageFailed(FailedObject obj)
            {
                failedQueue.Enqueue(obj);
            }

            private async Task<DownloadResponse> ExportObject(DownloadObject downloadObj)
            {
                var downloadClient = new Client(new SwiftAuthManager(credentials))
                        .SetRetryCount(2)
                        .SetLogger(new SwiftConsoleLog());

                using (var response = await downloadClient.GetObject(downloadObj.Container, downloadObj.Object.Object))
                {
                    if (response.IsSuccess)
                    {
                        using (Stream streamToWriteTo = File.OpenWrite(downloadObj.FilePath))
                        {
                            response.Stream.CopyTo(streamToWriteTo, bufferSize);
                        }

                        return new DownloadResponse
                        {
                            IsSuccess = true
                        };
                    }
                    else
                    {
                        return new DownloadResponse
                        {
                            IsSuccess = false,
                            Message = response.Reason
                        };
                    }
                }
            }

            public void EnqueueObjects(string containerName, List<SwiftObjectModel> objects, string path)
            {
                if (objects != null && objects.Count > 0)
                {
                    for (var i = 0; i < objects.Count; i++)
                    {
                        var obj = objects[i];

                        var objectName = obj.Object + "";

                        var filePath = Path.Combine(path, PathEscaper.Escape(objectName));

                        var skipFile = File.Exists(filePath) && File.OpenRead(filePath).Length == obj.Bytes;

                        if (skipFile) continue;

                        downloadBag.Add(new DownloadObject
                        {
                            Object = obj,
                            Container = containerName,
                            FilePath = filePath
                        });
                    }
                }
            }
        }


        public static int Run(ExportOptions options, Client client, AuthManager authManager)
        {
            string exportDir = options.Path ?? Directory.GetCurrentDirectory();

            if (string.IsNullOrEmpty(options.Container))
            {
                var accountData = client.GetAccount().Result;
                if (accountData.IsSuccess)
                {
                    if (accountData.Containers != null && accountData.Containers.Count > 0)
                    {
                        var exporter = new Exporter(client, authManager.Credentials());

                        if (string.IsNullOrEmpty(options.Prefix))
                        {
                            exporter.GetObjects(accountData.Containers, exportDir);
                        }
                        else
                        {
                            exporter.GetObjectsWithPrefix(options.Prefix, accountData.Containers, exportDir);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No containers found");
                    }
                }
                else
                {
                    Logger.LogError(accountData.Reason);
                }
            }

            return 0;
        }
    }
}

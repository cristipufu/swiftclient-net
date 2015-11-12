using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public static class ListCommand
    {
        public static int Run(ListOptions options, SwiftClient client)
        {
            if (string.IsNullOrEmpty(options.Container))
            {
                var accountData = client.GetAccount().Result;
                if (accountData.IsSuccess)
                {
                    if (accountData.Containers != null && accountData.Containers.Count > 0)
                    {
                        var list = new List<SwiftContainer>();
                        foreach (var c in accountData.Containers)
                        {
                            list.Add(new SwiftContainer
                            {
                                Bytes = c.Bytes,
                                Container = c.Container,
                                Objects = c.Objects
                            });
                        }

                        var table = list.ToStringTable(
                            u => u.Container,
                            u => u.Objects,
                            u => u.Size
                        );
                        Console.WriteLine(table);
                    }
                    else
                    {
                        Console.WriteLine("No containers found");
                    }
                }
                else
                {
                    Console.WriteLine(accountData.Reason);
                }
            }
            else
            {
                var containerData = client.GetContainer(options.Container).Result;
                if (containerData.IsSuccess)
                {
                    if (containerData.Objects != null && containerData.Objects.Count > 0)
                    {
                        var list = new List<SwiftObject>();
                        foreach (var c in containerData.Objects)
                        {
                            list.Add(new SwiftObject
                            {
                                Bytes = c.Bytes,
                                ContentType = c.ContentType,
                                Hash = c.Hash,
                                LastModified = c.LastModified,
                                Object = c.Object
                            });
                        }

                        var table = list.ToStringTable(
                            u => u.Object,
                            u => u.Size,
                            u => u.LastModified
                        );
                        Console.WriteLine(table);
                    }
                    else
                    {
                        Console.WriteLine($"No objects found in container {options.Container}");
                    }
                }
                else
                {
                    Console.WriteLine(containerData.Reason);
                }
            }
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public static class StatsCommand
    {
        public static int Run(StatsOptions options, Client client)
        {
                var accountData = client.GetAccountAsync().Result;
                if (accountData.IsSuccess)
                {

                var stats = new SwiftAccountStats
                {
                    ContainersCount = accountData.ContainersCount,
                    ObjectsCount = accountData.ObjectsCount,
                    TotalBytes = accountData.TotalBytes,
                };
                var table = new List<SwiftAccountStats> { stats }.ToStringTable(
                 u => u.ContainersCount,
                 u => u.ObjectsCount,
                 u => u.Size
                );
                Console.WriteLine(table);
            }
                else
                {
                    Logger.LogError(accountData.Reason);
                }
            
            return 0;
        }
    }
}

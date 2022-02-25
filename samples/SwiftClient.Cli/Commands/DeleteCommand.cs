using System;

namespace SwiftClient.Cli
{
    public static class DeleteCommand
    {
        public static int Run(DeleteOptions options, Client client)
        {
            if (string.IsNullOrEmpty(options.Object))
            {
                var response = client.DeleteContainerWithContentsAsync(options.Container, options.Limit).Result;
                if (response.IsSuccess)
                {
                    Console.WriteLine($"{options.Container} deleted");
                }
                else
                {
                    Console.WriteLine(response.Reason);
                    return 404;
                }
            }
            else
            {
                var response = client.DeleteObjectAsync(options.Container, options.Object).Result;
                if (response.IsSuccess)
                {
                    Console.WriteLine($"{options.Container}/{options.Object} deleted");
                }
                else
                {
                    Logger.LogError(response.Reason);
                    return 404;
                }
            }

            
            return 0;
        }
    }
}

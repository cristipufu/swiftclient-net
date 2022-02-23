using Humanizer.Bytes;
using System;
using System.IO;

namespace SwiftClient.Cli
{
    public static class GetCommand
    {
        public static int Run(GetOptions options, Client client)
        {
            int bufferSize = Convert.ToInt32(ByteSize.FromMegabytes(options.BufferSize).Bytes);
            var headObject = client.HeadObjectAsync(options.Container, options.Object).Result;

            if (headObject.IsSuccess)
            {
                using (var response = client.GetObjectAsync(options.Container, options.Object).Result)
                {
                    using (Stream streamToWriteTo = File.OpenWrite(options.File))
                    {
                        response.Stream.CopyTo(streamToWriteTo, bufferSize);
                    }
                }

                Console.WriteLine($"{options.Container}/{options.Object} downloaded to {options.File} ");
                return 0;
            }
            else
            {
                Logger.LogError(headObject.Reason);
                return 404;
            }
        }
    }
}

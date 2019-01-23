using System;
using SwiftClient;
using SwiftClient.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SwiftClientServiceCollectionExtensions
    {

        public static IServiceCollection AddSwift(
            this IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddHttpClient("swift");
            serviceCollection.AddOptions();
            serviceCollection.AddSingleton<ISwiftLogger, SwiftServiceLogger>();
            serviceCollection.AddSingleton<ISwiftAuthManager, SwiftAuthManagerMemoryCache>();
            serviceCollection.AddTransient<ISwiftClient, SwiftService>();

            return serviceCollection;
        }

        public static IServiceCollection AddSwift(
            this IServiceCollection serviceCollection,
            Action<SwiftServiceOptions> configure)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            serviceCollection.Configure(configure);
            return serviceCollection.AddSwift();
        }
    }
}
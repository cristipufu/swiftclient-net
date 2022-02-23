using System;
using SwiftClient;
using SwiftClient.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SwiftClientServiceCollectionExtensions
    {

        public static IServiceCollection AddSwift(this IServiceCollection serviceCollection, string httpClientName = "swift")
        {
            _ = serviceCollection ?? throw new ArgumentNullException(nameof(serviceCollection));

            serviceCollection.AddHttpClient(httpClientName);
            serviceCollection.AddOptions();
            serviceCollection.AddSingleton<ISwiftLogger, SwiftServiceLogger>();
            serviceCollection.AddSingleton<ISwiftAuthManager, SwiftAuthManagerMemoryCache>();
            serviceCollection.AddTransient<ISwiftClient, SwiftService>();

            return serviceCollection;
        }

        public static IServiceCollection AddSwift(
            this IServiceCollection serviceCollection,
            Action<SwiftServiceOptions> configure,
            string httpClientName = "swift")
        {
            _ = serviceCollection ?? throw new ArgumentNullException(nameof(serviceCollection));
            _ = configure ?? throw new ArgumentNullException(nameof(configure));

            serviceCollection.Configure(configure);
            return serviceCollection.AddSwift(httpClientName);
        }
    }
}
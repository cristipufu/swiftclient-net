using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SwiftClient.Test
{
    public static class CustomConfigReader
    {
        public static T Get<T>(IConfiguration section) where T : class, new()
        {
            var services = new ServiceCollection();
            services.Configure<T>(section);

            T obj = new T();

            (services[0].ImplementationInstance as ConfigureFromConfigurationOptions<T>).Action.Invoke(obj);

            return obj;
        }
    }
}

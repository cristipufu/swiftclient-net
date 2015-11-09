using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace SwiftClient.Test
{
    public static class CustomConfigReader
    {
        public static T Get<T>(IConfiguration section) where T : new()
        {
            var services = new ServiceCollection();
            services.Configure<T>(section);

            T obj = new T();

            (services[0].ImplementationInstance as ConfigureFromConfigurationOptions<T>).Action.Invoke(obj);

            return obj;
        }
    }
}

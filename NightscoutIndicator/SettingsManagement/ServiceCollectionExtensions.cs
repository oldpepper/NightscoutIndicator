using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NightscoutIndicator.SettingsManagement;

public static class ServiceCollectionExtensions
{
    public static void ConfigureWritable<T>(
        this IServiceCollection services,
        IConfigurationSection section) where T : class, new()
    {
        services.Configure<T>(section);
        services.AddSingleton<IWritableOptions<T>>(provider =>
        {
//            var environment = provider.GetService<IHostEnvironment>();
            var options = provider.GetService<IOptionsSnapshot<T>>();
            var configuration = provider.GetService<IConfigurationRoot>();
            return new WritableOptions<T>(
//                                          environment,
                                          options, 
                                          configuration, 
                                          section.Key);
        });
    }
}

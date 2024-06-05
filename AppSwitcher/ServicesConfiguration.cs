using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace AppSwitcher;

internal static class ServicesConfiguration
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddNLog());

        services.AddSingleton<MainWindow>();
        services.AddSingleton<ConfigurationReader>();
        services.AddSingleton<ConfigurationValidator>();
        services.AddSingleton<Hook>();
        services.AddSingleton<WindowHelper>();

        return services.BuildServiceProvider();
    }
}

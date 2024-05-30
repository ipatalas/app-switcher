using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace AppSwitcher;

internal static class ServicesConfiguration
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddLogging(logging => logging.AddNLog());

        services.AddSingleton<MainWindow>();
        services.AddSingleton<ConfigurationReader>();
        services.AddSingleton<ConfigurationValidator>();
        services.AddSingleton<Hook>();
        services.AddSingleton<WindowHelper>();
    }
}

using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.Utils;
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

        services.AddTransient<MainWindow>();
        services.AddTransient<ConfigurationReader>();
        services.AddTransient<ConfigurationValidator>();
        services.AddTransient<ConfigurationManager>();
        services.AddTransient<Hook>();
        services.AddTransient<WindowHelper>();
        services.AddTransient<Switcher>();
        services.AddTransient<AutoStart>();
        services.AddTransient<ModifierIdleTimer>();

        services.AddCliHandler();

        return services.BuildServiceProvider();
    }
}

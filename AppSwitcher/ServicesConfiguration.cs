using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.Utils;
using AppSwitcher.ViewModels;
using AppSwitcher.WindowDiscovery;
using AppSwitcher.Windows;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace AppSwitcher;

internal static class ServicesConfiguration
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddNLog());

        services.AddTransient<ConfigurationReader>();
        services.AddTransient<ConfigurationValidator>();
        services.AddSingleton<ConfigurationManager>();
        services.AddTransient<Hook>();
        services.AddTransient<WindowHelper>();
        services.AddTransient<Switcher>();
        services.AddTransient<AutoStart>();
        services.AddTransient<ModifierIdleTimer>();

        services.AddCliHandler();

        // windows
        services.AddTransient<MainWindow>();
        services.AddTransient<Settings>();

        // view models
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}

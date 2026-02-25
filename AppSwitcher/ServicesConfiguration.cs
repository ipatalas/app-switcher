using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.WindowDiscovery;
using AppSwitcher.UI.Windows;
using AppSwitcher.Utils;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Wpf.Ui.Abstractions;

namespace AppSwitcher;

internal static class ServicesConfiguration
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddNLog());

        services.AddTransient<INavigationViewPageProvider, PageProviderService>();

        services.AddTransient<ConfigurationService>();
        services.AddTransient<ConfigurationValidator>();
        services.AddSingleton<ConfigurationManager>();
        services.AddTransient<Hook>();
        services.AddTransient<WindowHelper>();
        services.AddTransient<Switcher>();
        services.AddTransient<AutoStart>();
        services.AddSingleton<IconExtractor>();
        services.AddTransient<ModifierIdleTimer>();

        services.AddCliHandler();

        // windows
        services.AddTransient<MainWindow>();
        services.AddTransient<Settings>();

        // pages
        services.AddTransient<Hotkeys>();
        services.AddTransient<About>();

        // view models
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}

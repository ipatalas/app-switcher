using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.WindowDiscovery;
using AppSwitcher.UI.Windows;
using AppSwitcher.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NLog.Extensions.Logging;
using System.Reflection;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace AppSwitcher;

internal static class ServicesConfiguration
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddNLog());

        services.AddTransient<INavigationViewPageProvider, PageProviderService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();

        services.AddTransient<ConfigurationService>();
        services.AddTransient<ConfigurationValidator>();
        services.AddSingleton<ConfigurationManager>();
        services.AddTransient<Hook>();
        services.AddTransient<WindowHelper>();
        services.AddTransient<Switcher>();
        services.AddTransient<AutoStart>();
        services.AddSingleton<IconExtractor>();
        services.AddTransient<ModifierIdleTimer>();
        services.AddTransient<ProcessPathExtractor>();
        services.AddSingleton<AppLocator>();
        services.AddTransient<RunningApplicationsService>();

        services.AddCliHandler();

        // windows
        services.AddTransient<MainWindow>();
        services.AddTransient<Settings>();

        // pages
        services.AddImplementationsOf<Page>(ServiceLifetime.Transient);

        // view models
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<AddApplicationFlyoutViewModel>();
        services.AddTransient<AboutViewModel>();

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddImplementationsOf<TInterface>(this IServiceCollection services, ServiceLifetime lifetime)
    {
        var interfaceType = typeof(TInterface);
        var implementations = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (var implementation in implementations)
        {
            services.TryAdd(new ServiceDescriptor(implementation, implementation, lifetime));
        }

        return services;
    }
}

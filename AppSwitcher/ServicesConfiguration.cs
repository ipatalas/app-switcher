using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.Configuration.Migrations;
using AppSwitcher.Extensions;
using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.WindowDiscovery;
using AppSwitcher.UI.Windows;
using AppSwitcher.Utils;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NLog.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace AppSwitcher;

internal static class ServicesConfiguration
{
    public static IServiceProvider? Build(bool isPortableMode)
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddNLog());

        if (!services.SetupLiteDb(isPortableMode))
        {
            return null;
        }

        services.AddTransient<INavigationViewPageProvider, PageProviderService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();

        services.AddTransient<ConfigurationService>();
        services.AddTransient<ConfigurationValidator>();
        services.AddTransient<MigrationRunner>();
        services.AddImplementationsOf<IMigration>(ServiceLifetime.Transient);
        services.AddSingleton<ConfigurationManager>();
        services.AddSingleton<Hook>();
        services.AddTransient<WindowHelper>();
        services.AddTransient<Switcher>();
        services.AddTransient<AutoStart>();
        services.AddSingleton<IconExtractor>();
        services.AddTransient<ModifierIdleTimer>();
        services.AddTransient<OverlayShowTimer>();
        services.AddTransient<IProcessPathExtractor, ProcessPathExtractor>();
        services.AddTransient<RunningApplicationsService>();
        services.AddTransient<IPackagedAppsService, PackagedAppsService>();
        services.AddTransient<TitleSuffixHelper>();
        services.AddTransient<AppOverlayService>();

        services.AddCliHandler();

        // windows
        services.AddTransient<MainWindow>();
        services.AddTransient<Settings>();
        services.AddSingleton<AppOverlayWindow>();

        // pages
        services.AddImplementationsOf<Page>(ServiceLifetime.Transient, registerAsConcreteType: true);

        // view models
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<AddApplicationFlyoutViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddSingleton<AppOverlayViewModel>();

        return services.BuildServiceProvider();
    }

    private static bool SetupLiteDb(this ServiceCollection services, bool isPortableMode)
    {
        BsonMapper.Global.EnumAsInteger = true;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = isPortableMode
            ? Path.Combine(AppContext.BaseDirectory, "settings.db")
            : Path.Combine(appData, "AppSwitcher", "settings.db");

        try
        {
            var db = new LiteDatabase(new ConnectionString(dbPath) { Connection = ConnectionType.Direct });
            services.AddSingleton(db);
            return true;
        }
        catch (Exception)
        {
            new Wpf.Ui.Controls.MessageBox
            {
                Title = "Database error",
                Content = $"An error occurred while reading the settings.\nFile might be corrupted. Please remove it and start over.\n\nPath:\n{dbPath}",
                CloseButtonIcon = new SymbolIcon(SymbolRegular.ErrorCircle24),
                CloseButtonText = "Quit",
                CloseButtonAppearance = ControlAppearance.Danger,
            }.ShowSync();
            return false;
        }
    }

    private static IServiceCollection AddImplementationsOf<TInterface>(this IServiceCollection services, ServiceLifetime lifetime, bool registerAsConcreteType = false)
    {
        var interfaceType = typeof(TInterface);
        var implementations = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (var implementation in implementations)
        {
            var serviceType = registerAsConcreteType ? implementation : interfaceType;
            var serviceDescriptor = new ServiceDescriptor(serviceType, implementation, lifetime);

            if (registerAsConcreteType)
            {
                services.TryAdd(serviceDescriptor);
            }
            else
            {
                // Allow multiple implementations for the same interface to be registered.
                services.Add(serviceDescriptor);
            }
        }

        return services;
    }
}

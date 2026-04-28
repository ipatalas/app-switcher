using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.Configuration.Migrations;
using AppSwitcher.Extensions;
using AppSwitcher.Input;
using AppSwitcher.Overlay;
using AppSwitcher.Startup;
using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.ViewModels.Common;
using AppSwitcher.WindowDiscovery;
using AppSwitcher.UI.Windows;
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

        if (!services.SetupDatabases(isPortableMode))
        {
            return null;
        }

        services.AddTransient<INavigationViewPageProvider, PageProviderService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();

        services.AddTransient<ConfigurationService>();
        services.AddTransient<ConfigurationValidator>();
        services.AddTransient<ApplicationsValidator>();
        services.AddTransient<PackagedAppPathSanitizer>();
        services.AddTransient<MigrationRunner>();
        services.AddImplementationsOf<IMigration>(ServiceLifetime.Transient);
        services.AddSingleton<ConfigurationManager>();
        services.AddSingleton<Hook>();
        services.AddSingleton<DynamicModeService>();
        services.AddSingleton<IAppNameResolver, AppNameResolver>();
        services.AddSingleton<Peeker>();
        services.AddTransient<Switcher>();
        services.AddTransient<AutoStart>();
        services.AddSingleton<IconExtractor>();
        services.AddTransient<OverlayShowTimer>();
        services.AddTransient<IProcessPathExtractor, ProcessPathExtractor>();
        services.AddTransient<RunningApplicationsService>();
        services.AddTransient<IPackagedAppsService, PackagedAppsService>();
        services.AddSingleton<IWindowEnumerator, WindowEnumerator>();
        services.AddTransient<WindowTitleParser>();
        services.AddTransient<AppOverlayService>();
        services.AddSingleton<WarningOverlayService>();
        services.AddTransient<IProcessInspector, ProcessInspector>();

        services.AddSingleton<SessionStats>();
        services.AddSingleton<AppRegistryCache>();
        services.AddSingleton<IAppRegistryCache>(sp => sp.GetRequiredService<AppRegistryCache>());
        services.AddSingleton<StatsService>();
        services.AddTransient<StatsRepository>();
        services.AddTransient<StatsCalculator>();

        services.AddCliHandler();

        // windows
        services.AddTransient<MainWindow>();
        services.AddTransient<Settings>();
        services.AddSingleton<AppOverlayWindow>();
        services.AddSingleton<WarningOverlayWindow>();

        // pages
        services.AddImplementationsOf<Page>(ServiceLifetime.Transient, registerAsConcreteType: true);

        // view models
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<ISettingsState, SettingsState>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HotkeysViewModel>();
        services.AddTransient<GeneralSettingsViewModel>();
        services.AddTransient<OverlaySettingsViewModel>();
        services.AddTransient<StatsSettingsViewModel>();
        services.AddTransient<AddApplicationFlyoutViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddSingleton<AppOverlayViewModel>();

        return services.BuildServiceProvider();
    }

    private static bool SetupDatabases(this ServiceCollection services, bool isPortableMode)
    {
        BsonMapper.Global.EnumAsInteger = true;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var mainDbPath = GetDbPath("settings.db");
        var statsDbPath = GetDbPath("stats.db");

        try
        {
            var mainDb = new LiteDatabase(new ConnectionString(mainDbPath) { Connection = ConnectionType.Direct });
            services.AddSingleton(mainDb);

            // stats db is lazy loaded so that when user has it disabled stats.db file will not appear until it's enabled again
            services.AddSingleton(new StatsDbProvider(() =>
                new LiteDatabase(new ConnectionString(statsDbPath) { Connection = ConnectionType.Direct })));

            return true;
        }
        catch (Exception)
        {
            new MessageBox
            {
                Title = "Database error",
                Content = $"An error occurred while reading the settings.\nFile might be corrupted. Please remove it and start over.\n\nPath:\n{mainDbPath}",
                CloseButtonIcon = new SymbolIcon(SymbolRegular.ErrorCircle24),
                CloseButtonText = "Quit",
                CloseButtonAppearance = ControlAppearance.Danger,
            }.ShowSync();
            return false;
        }

        string GetDbPath(string name)
        {
            return isPortableMode
                ? Path.Combine(AppContext.BaseDirectory, name)
                : Path.Combine(appData, "AppSwitcher", name);
        }
    }

    private static void AddImplementationsOf<TInterface>(this IServiceCollection services, ServiceLifetime lifetime,
        bool registerAsConcreteType = false)
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
    }
}
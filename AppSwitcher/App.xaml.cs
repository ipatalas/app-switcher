using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.Extensions;
using AppSwitcher.Input;
using AppSwitcher.Stats;
using AppSwitcher.UI.Windows;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace AppSwitcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly string _portableFilePath = Path.Combine(AppContext.BaseDirectory, ".portable");
    private Hook? _hook;
    private StatsService? _statsService;
    private IServiceProvider? _serviceProvider;
#if !DEBUG
    private Mutex? _mutex;
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
#if !DEBUG
        _mutex = new Mutex(true, "AppSwitcherMutex", out var createdNew);
        if (!createdNew)
        {
            new MessageBox
            {
                Title = "AppSwitcher",
                Content = "Another instance of AppSwitcher is already running",
                CloseButtonIcon = new SymbolIcon(SymbolRegular.Info24),
                CloseButtonText = "OK",
            }.ShowSync();
            Current.Shutdown(0);
            return;
        }
#endif
        bool isPortableMode = File.Exists(_portableFilePath);

        _serviceProvider = ServicesConfiguration.Build(isPortableMode);
        if (_serviceProvider == null)
        {
            Current.Shutdown(1);
            return;
        }

        HandleUnhandledExceptions();

        var configuration = NLog.LogManager.Configuration;
        if (configuration != null)
        {
            configuration.Variables["logDir"] = GetLogDir(isPortableMode);
        }

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("AppSwitcher {Version} starting up (Portable mode = {Portable})", AppVersion.Version,
            isPortableMode);

        var cliHandler = _serviceProvider.GetRequiredService<CliHandler>();
        if (cliHandler.Handle(e.Args))
        {
            Current.Shutdown(0);
            return;
        }

        var cliOptions = _serviceProvider.GetRequiredService<CliOptions>();
        SetupLogging(cliOptions, logger);

        var configManager = _serviceProvider.GetRequiredService<ConfigurationManager>();
        var config = configManager.GetConfiguration();
        if (config == null)
        {
            new MessageBox
            {
                Title = "Configuration error",
                Content =
                    "Configuration could not be loaded or is invalid. Please check the logs for more details.",
                CloseButtonIcon = new SymbolIcon(SymbolRegular.ErrorCircle24),
                CloseButtonText = "Quit",
                CloseButtonAppearance = ControlAppearance.Danger,
            }.ShowSync();
            Current.Shutdown(1);
            return;
        }

        ApplyTheme(config.Theme);

        Current.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        Current.MainWindow.Show();

        // Eagerly create the overlay window singleton so it is ready before the hook starts.
        _serviceProvider.GetRequiredService<AppOverlayWindow>();

        _hook = _serviceProvider.GetRequiredService<Hook>();
        _hook.Start(config);

        _statsService = _serviceProvider.GetRequiredService<StatsService>();
        _statsService.Start(config.StatsEnabled);

        configManager.ConfigurationChanged += newConfig =>
        {
            _hook?.UpdateConfiguration(newConfig);
            _statsService?.UpdateConfiguration(newConfig);
            ApplyTheme(newConfig.Theme);
        };
    }

    private void HandleUnhandledExceptions()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
            logger?.LogError(ex, "Unhandled exception occurred");

            Current.Dispatcher.Invoke(() =>
            {
                new MessageBox
                {
                    Title = "Unexpected error",
                    Content = "An unexpected error occurred. Please check the logs for more details.",
                    CloseButtonIcon = new SymbolIcon(SymbolRegular.ErrorCircle24),
                    CloseButtonText = "Quit",
                    CloseButtonAppearance = ControlAppearance.Danger,
                }.ShowSync();
                Current.Shutdown(1);
            });
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
        logger?.LogInformation("AppSwitcher shutting down");

        _statsService?.Dispose();
        // This has to be disposed manually because service was created manually
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines#services-not-created-by-the-service-container
        _serviceProvider?.GetRequiredService<LiteDatabase>().Dispose();

        (_serviceProvider as IDisposable)?.Dispose();
#if !DEBUG
        _mutex?.Dispose();
#endif
    }

    private static void ApplyTheme(AppThemeSetting theme)
    {
        if (theme == AppThemeSetting.System)
        {
            ApplicationThemeManager.ApplySystemTheme();
        }
        else
        {
            var applicationTheme = theme == AppThemeSetting.Dark
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(applicationTheme);
        }
    }

    private static void SetupLogging(CliOptions cliOptions, ILogger<App> logger)
    {
        if (cliOptions.EnableDebugLogging || cliOptions.EnableTraceLogging)
        {
            if (NLog.LogManager.Configuration?.LoggingRules != null)
            {
                foreach (var rule in NLog.LogManager.Configuration.LoggingRules)
                {
                    rule.EnableLoggingForLevel(
                        cliOptions.EnableTraceLogging ? NLog.LogLevel.Trace : NLog.LogLevel.Debug);
                }
            }

            NLog.LogManager.ReconfigExistingLoggers();
            logger.LogInformation("Logging level set to {Level} via CLI options",
                cliOptions.EnableTraceLogging ? "Trace" : "Debug");
        }
    }

    private static string GetLogDir(bool isPortableMode)
    {
        if (isPortableMode)
        {
            return Path.Combine(AppContext.BaseDirectory, "logs");
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "AppSwitcher", "logs");
    }
}
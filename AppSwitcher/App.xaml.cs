using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.Extensions;
using AppSwitcher.UI.Windows;
using AppSwitcher.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using Wpf.Ui.Controls;

namespace AppSwitcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private Hook? _hook;
    private IServiceProvider? _serviceProvider;
#if !DEBUG
    private Mutex? _mutex;
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
        _serviceProvider = ServicesConfiguration.Build();
        if (_serviceProvider == null)
        {
            Current.Shutdown(1);
            return;
        }

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("AppSwitcher {Version} starting up", AppVersion.Version);

        var cliHandler = _serviceProvider.GetRequiredService<CliHandler>();
        if (cliHandler.Handle(e.Args))
        {
            Current.Shutdown(0);
            return;
        }

        var cliOptions = _serviceProvider.GetRequiredService<CliOptions>();
        SetupLogging(cliOptions, logger);

#if !DEBUG
        _mutex = new Mutex(true, "AppSwitcherMutex", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("AppSwitcher is already running", "AppSwitcher", MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown(0);
            return;
        }
#endif

        var configManager = _serviceProvider.GetRequiredService<ConfigurationManager>();
        var config = configManager.GetConfiguration();
        if (config == null)
        {
            new Wpf.Ui.Controls.MessageBox
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

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        _hook = _serviceProvider.GetRequiredService<Hook>();
        _hook.Start(config);

        configManager.ConfigurationChanged += newConfig =>
        {
            _hook?.UpdateConfiguration(newConfig);
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
        logger?.LogInformation("AppSwitcher shutting down");

        _hook?.Dispose();
        (_serviceProvider as ServiceProvider)?.Dispose();
#if !DEBUG
        _mutex?.Dispose();
#endif
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
}

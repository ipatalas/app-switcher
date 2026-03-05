using AppSwitcher.CLI;
using AppSwitcher.Configuration;
using AppSwitcher.UI.Windows;
using AppSwitcher.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace AppSwitcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private Hook? _hook;
#if !DEBUG
    private Mutex? _mutex;
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceProvider = ServicesConfiguration.Build();

        var logger = serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("AppSwitcher {Version} starting up", AppVersion.Version);

        var cliHandler = serviceProvider.GetRequiredService<CliHandler>();
        if (cliHandler.Handle(e.Args))
        {
            Current.Shutdown(0);
            return;
        }

        var cliOptions = serviceProvider.GetRequiredService<CliOptions>();
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

        var configManager = serviceProvider.GetRequiredService<ConfigurationManager>();
        var config = configManager.GetConfiguration();
        if (config == null)
        {
            MessageBox.Show("Configuration file (config.json) not found or invalid", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown(1);
            return;
        }

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        _hook = serviceProvider.GetRequiredService<Hook>();
        _hook.Start(config);

        configManager.ConfigurationChanged += newConfig =>
        {
            _hook?.UpdateConfiguration(newConfig);
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
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
                rule.EnableLoggingForLevel(cliOptions.EnableTraceLogging ? NLog.LogLevel.Trace : NLog.LogLevel.Debug);
            }
            }

            NLog.LogManager.ReconfigExistingLoggers();
            logger.LogInformation("Logging level set to {Level} via CLI options",
                cliOptions.EnableTraceLogging ? "Trace" : "Debug");
        }
    }
}

using AppSwitcher.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
#if DEBUG
        logger.LogInformation("AppSwitcher [DEBUG] starting up");
#else
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        logger.LogInformation("AppSwitcher v{Version} starting up", version);
#endif

        var cliHandler = serviceProvider.GetRequiredService<CliHandler>();
        if (cliHandler.Handle(e.Args))
        {
            Current.Shutdown(0);
            return;
        }

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
        FixMainWindowWhenDebuggerAttached(mainWindow);

        _hook = serviceProvider.GetRequiredService<Hook>();
        _hook.Start(config);

        configManager.ConfigurationChanged += newConfig =>
        {
            _hook?.UpdateConfiguration(newConfig);
        };

#if DEBUG
        var trayIconTitle = "[DEBUG] Click to close AppSwitcher";
#else
        var trayIconTitle = "Click to close AppSwitcher";
#endif
        NotifyIcon trayIcon = new()
        {
            Icon = ProjectResources.app_switcher,
            Visible = true,
            Text = trayIconTitle
        };

        trayIcon.Click += (_, _) => Current.Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
#if !DEBUG
        _mutex?.Dispose();
#endif
    }

    [Conditional("DEBUG")]
    private void FixMainWindowWhenDebuggerAttached(MainWindow mainWindow)
    {
        if (!Debugger.IsAttached)
        {
            return;
        }

        // Hacky but if main window is never shown during app lifetime in debug mode, application shutdown will take 4-5 seconds
        // Works good when debugger is not attached
        mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        mainWindow.Left = -10000;
        mainWindow.Top = -10000;
        mainWindow.Show();
        mainWindow.Hide();
    }
}

using AppSwitcher.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceProvider = ServicesConfiguration.Build();

        var cliHandler = serviceProvider.GetRequiredService<CliHandler>();
        if (cliHandler.Handle(e.Args))
        {
            Current.Shutdown(0);
            return;
        }

        _mutex = new Mutex(true, "AppSwitcherMutex", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("AppSwitcher is already running", "AppSwitcher", MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown(0);
            return;
        }

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

        NotifyIcon trayIcon = new()
        {
            Icon = ProjectResources.app_switcher,
            Visible = true,
            Text = @"Click to close AppSwitcher"
        };

        trayIcon.Click += (_, _) => Current.Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        _mutex?.Dispose();
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

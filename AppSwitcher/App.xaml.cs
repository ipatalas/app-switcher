using AppSwitcher.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace AppSwitcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
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

        
        if (!GetConfiguration(serviceProvider, out var config))
        {
            Current.Shutdown(1);
            return;
        }

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        FixMainWindowWhenDebuggerAttached(mainWindow);

        _hook = serviceProvider.GetRequiredService<Hook>();
        _hook.Start(config);

        NotifyIcon trayIcon = new()
        {
            Icon = ProjectResources.AppIcon,
            Visible = true,
            Text = "Click to close AppSwitcher"
        };

        trayIcon.Click += (sender, e) => Current.Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        _mutex?.Dispose();
    }

    private bool GetConfiguration(IServiceProvider serviceProvider, [NotNullWhen(true)] out Configuration.Configuration? configuration)
    {
        configuration = null;
        var configReader = serviceProvider.GetRequiredService<ConfigurationReader>();
        var configValidator = serviceProvider.GetRequiredService<ConfigurationValidator>();

        if (configReader.ConfigurationExists() is false)
        {
            MessageBox.Show("Configuration file (config.json) not found. Please create one and restart AppSwitcher\nYou can use config.json.example as an example", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var config = configReader.ReadConfiguration();
        if (config is null)
        {
            MessageBox.Show("Error reading configuration file - see logs for details", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        else if (configValidator.ValidateAndLog(config) is { Status: ValidationResultStatus.Error } result)
        {
            MessageBox.Show($"{result.Message}\nFix the error and run AppSwitcher again", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        configuration = config;

        return true;
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

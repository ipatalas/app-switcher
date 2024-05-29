using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using System.Diagnostics;
using System.Windows;

namespace AppSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private Hook? hook;

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
#if DEBUG
            InitializeMainWindow(mainWindow);
#endif

            hook = serviceProvider.GetRequiredService<Hook>();
            hook.Start();

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
            hook?.Dispose();
        }

#if DEBUG
        private void InitializeMainWindow(MainWindow mainWindow)
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
#endif

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging => logging.AddNLog());

            services.AddTransient<MainWindow>();
            services.AddTransient<Hook>();
            services.AddTransient<WindowHelper>();
        }
    }
}

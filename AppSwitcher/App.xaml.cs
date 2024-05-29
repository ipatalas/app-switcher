using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Windows;

namespace AppSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Hook? hook;

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            hook = serviceProvider.GetRequiredService<Hook>();
            hook.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            hook?.Dispose();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging => logging.AddNLog());

            services.AddTransient<MainWindow>();
            services.AddTransient<Hook>();
            services.AddTransient<WindowHelper>();
        }
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia_MVVM.ViewModels;
using Avalonia_MVVM.Views;
using KeyboardHookLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;


namespace Avalonia_MVVM
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = GetServices();
            var provider = services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<App>>();
            var hook = provider.GetRequiredService<Hook>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                logger.LogInformation("Starting application");
                hook.Start();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                

                desktop.Exit += (sender, e) => hook.Dispose();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private ServiceCollection GetServices()
        {
            var services = new ServiceCollection();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<Hook>();
            services.AddTransient<WindowHelper>();
            services.AddLogging(logging =>
            {
                logging.AddDebug();
                if (!Debugger.IsAttached)
                {
                    logging.AddNLog();
                }
            });

            return services;
        }

        
    }
}
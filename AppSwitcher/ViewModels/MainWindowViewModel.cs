using AppSwitcher.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Application = System.Windows.Application;

namespace AppSwitcher.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Settings? _settings;

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _logger.LogInformation("MainWindowViewModel initialized");
    }

    public MainWindowViewModel() : this(NullLogger<MainWindowViewModel>.Instance, null!)
    {
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _logger.LogInformation("Opening Settings window");

        if (_settings == null || !_settings.IsLoaded)
        {
            _settings = _serviceProvider.GetRequiredService<Settings>();
            _settings.Closed += (sender, e) => _settings = null; // Clear reference when closed
            _settings.Show();
        }
        else
        {
            _settings.Activate();
            _settings.Focus();
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        _logger.LogInformation("Exiting application");
        Application.Current.Shutdown();
    }
}
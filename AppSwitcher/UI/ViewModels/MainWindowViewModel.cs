using AppSwitcher.Extensions;
using AppSwitcher.UI.Windows;
using AppSwitcher.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace AppSwitcher.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Settings? _settings;

    public string TrayTooltipText
    {
        get
        {
            return "AppSwitcher " + AppVersion.Version;
        }
    }

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

        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in OpenSettings");
#endif
            if (_settings is not { IsLoaded: true })
            {
                _settings = _serviceProvider.GetRequiredService<Settings>();
                _settings.Closed += (_, _) => _settings = null;
                _settings.Show();
            }
            else
            {
                _settings.Activate();
                _settings.Focus();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Settings window");
            new MessageBox
            {
                Title = "AppSwitcher",
                Content = "Could not open the Settings window. Please try again.",
                CloseButtonIcon = new SymbolIcon(SymbolRegular.ErrorCircle24),
                CloseButtonText = "OK",
                CloseButtonAppearance = ControlAppearance.Danger,
            }.ShowSync();
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        _logger.LogInformation("Exiting application");
        Application.Current.Shutdown();
    }
}
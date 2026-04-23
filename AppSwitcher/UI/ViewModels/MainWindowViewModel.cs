using AppSwitcher.Extensions;
using AppSwitcher.Stats;
using AppSwitcher.UI.ViewModels.Common;
using AppSwitcher.UI.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace AppSwitcher.UI.ViewModels;

internal partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsState _settingsState;
    private readonly StatsService _statsService;
    private Settings? _settings;

    public string TrayTooltipText
    {
        get
        {
            return "AppSwitcher " + AppVersion.Version;
        }
    }

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, IServiceProvider serviceProvider,
        ISettingsState settingsState, StatsService statsService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settingsState = settingsState;
        _statsService = statsService;
        _logger.LogInformation("MainWindowViewModel initialized");
    }

    public MainWindowViewModel() : this(NullLogger<MainWindowViewModel>.Instance, null!, null!, null!)
    {
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _logger.LogDebug("Opening Settings window");

        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in OpenSettings");
#endif
            _statsService.Flush("Settings opened");

            // refresh every time Settings window is open
            // this will also fetch fresh icon for packaged apps (in case they were updated in the meantime)
            _settingsState.LoadConfiguration();

            if (_settings is not { IsLoaded: true })
            {
                _settings = _serviceProvider.GetRequiredService<Settings>();
                _settings.Owner = Application.Current.MainWindow;
                _settings.Closed += (_, _) => _settings = null;
                _settings.Show();
                _settings.Activate();
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
        _logger.LogInformation("Application exit requested");
        Application.Current.Shutdown();
    }
}
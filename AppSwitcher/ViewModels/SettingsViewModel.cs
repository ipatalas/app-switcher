using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AppSwitcher.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(ILogger<SettingsViewModel> logger)
    {
        _logger = logger;
        _logger.LogDebug("SettingsViewModel initialized");
    }
}
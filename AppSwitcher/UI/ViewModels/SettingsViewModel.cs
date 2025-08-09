using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModifierKeyDisplay))]
    private Key _modifierKey = Key.Apps;

    public string ModifierKeyDisplay => ModifierKey.ToString();


    public SettingsViewModel(ILogger<SettingsViewModel> logger)
    {
        _logger = logger;
        _logger.LogDebug("SettingsViewModel initialized");
    }
}
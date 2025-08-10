using AppSwitcher.Configuration;
using AppSwitcher.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

namespace AppSwitcher.UI.ViewModels;

internal partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModifierKeyDisplay))]
    private Key _modifierKey;

    public string ModifierKeyDisplay => ModifierKey.ToString();

    [ObservableProperty]
    private ObservableCollection<ApplicationConfigViewModel> _applications;

    protected SettingsViewModel()
    {
        // For design-time data
        _modifierKey = Key.Apps;
        _applications = [];
    }

    public SettingsViewModel(ConfigurationManager configurationManager, IconExtractor iconExtractor)
    {
        var config = configurationManager.GetConfiguration()!;
        _modifierKey = config.Modifier;

        var defaultIcon =
            iconExtractor.GetByProcessName(
                Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\shell32.dll");

        _applications = new ObservableCollection<ApplicationConfigViewModel>(config.Applications.Select(app => new ApplicationConfigViewModel
        {
            Key = app.Key,
            Name = app.Process,
            ProcessName = Path.GetFileName(app.NormalizedProcessName),
            StartIfNotRunning = app.StartIfNotRunning,
            CycleMode = app.CycleMode,
            ProcessIcon = iconExtractor.GetByProcessName(app.Process) ?? defaultIcon
        }).ToList());
    }

    [RelayCommand]
    private void RemoveApplication(ApplicationConfigViewModel application)
    {
        Applications.Remove(application);
    }
}

internal class ApplicationConfigViewModel
{
    public Key Key { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public bool StartIfNotRunning { get; set; }
    public CycleMode CycleMode { get; set; } = CycleMode.Default;

    public ImageSource? ProcessIcon { get; set; }
}
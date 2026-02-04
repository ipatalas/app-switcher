using AppSwitcher.Configuration;
using AppSwitcher.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

namespace AppSwitcher.UI.ViewModels;

internal partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ConfigurationManager _configurationManager = null!;
    private readonly IconExtractor _iconExtractor = null!;

    private SettingsViewModelDirtyTracker? _dirtyTracker;
    private SettingsSnapshot _originalSnapshot = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModifierKeyDisplay), nameof(IsDirty))]
    private Key _modifierKey = Key.Apps;

    public string ModifierKeyDisplay => ModifierKey.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private ObservableCollection<ApplicationShortcutViewModel> _applications = [];

    public bool IsDirty => !_originalSnapshot.Equals(CreateCurrentSnapshot());

    protected SettingsViewModel()
    {
    }

    public SettingsViewModel(ConfigurationManager configurationManager, IconExtractor iconExtractor)
    {
        _configurationManager = configurationManager;
        _iconExtractor = iconExtractor;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        var config = _configurationManager.GetConfiguration()!;
        ModifierKey = config.Modifier;

        _dirtyTracker?.Dispose();

        var defaultIcon =
            _iconExtractor.GetByProcessName(
                Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\shell32.dll");
        Applications = new ObservableCollection<ApplicationShortcutViewModel>(config.Applications.Select(app => new ApplicationShortcutViewModel
        {
            Key = app.Key,
            Name = app.Process,
            ProcessName = Path.GetFileName(app.NormalizedProcessName),
            StartIfNotRunning = app.StartIfNotRunning,
            CycleMode = app.CycleMode,
            ProcessIcon = _iconExtractor.GetByProcessName(app.Process) ?? defaultIcon
        }).ToList());

        _originalSnapshot = CreateCurrentSnapshot();
        _dirtyTracker = new SettingsViewModelDirtyTracker(this, () => OnPropertyChanged(nameof(IsDirty)));
    }

    private SettingsSnapshot CreateCurrentSnapshot()
    {
        return new SettingsSnapshot(ModifierKey,
            Applications.Select(app =>
                    new ApplicationShortcutSnapshot(app.Key, app.ProcessName, app.StartIfNotRunning, app.CycleMode))
                .ToList());
    }

    [RelayCommand]
    private void RemoveApplication(ApplicationShortcutViewModel application)
    {
        Applications.Remove(application);
    }

    [RelayCommand]
    private void SaveAndClose()
    {
        var newConfig = new Configuration.Configuration(ModifierKey, Applications.Select(app =>
            new ApplicationConfiguration(
                app.Key,
                app.ProcessName,
                app.CycleMode,
                app.StartIfNotRunning)).ToList());

        if (_configurationManager.UpdateConfiguration(newConfig))
        {
            _originalSnapshot = CreateCurrentSnapshot();
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    [RelayCommand]
    private void Reset()
    {
        LoadConfiguration();
    }

    public void Dispose()
    {
        _dirtyTracker?.Dispose();
    }
}

internal partial class ApplicationShortcutViewModel : ObservableObject
{
    [ObservableProperty] private Key _key;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private bool _startIfNotRunning;
    [ObservableProperty] private CycleMode _cycleMode = CycleMode.Default;

    public ImageSource? ProcessIcon { get; init; }
}
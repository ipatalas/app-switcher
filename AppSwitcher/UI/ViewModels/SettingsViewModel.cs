using AppSwitcher.Configuration;
using AppSwitcher.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.ViewModels;

internal partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ConfigurationManager _configurationManager = null!;
    private readonly IconExtractor _iconExtractor = null!;
    private readonly ISnackbarService _snackbarService = null!;
    private readonly ApplicationsValidator _validator = new();

    private SettingsViewModelDirtyTracker? _dirtyTracker;
    private SettingsSnapshot _originalSnapshot = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModifierKeyDisplay), nameof(IsDirty), nameof(CanSave))]
    private Key _modifierKey = Key.Apps;

    public string ModifierKeyDisplay => ModifierKey.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private ObservableCollection<ApplicationShortcutViewModel> _applications = [];

    partial void OnApplicationsChanged(
        ObservableCollection<ApplicationShortcutViewModel>? oldValue,
        ObservableCollection<ApplicationShortcutViewModel> newValue)
    {
        if (oldValue is not null)
        {
            oldValue.CollectionChanged -= OnApplicationsCollectionChanged;
        }

        newValue.CollectionChanged += OnApplicationsCollectionChanged;
        OnPropertyChanged(nameof(HasNoApplications));
    }

    private void OnApplicationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasNoApplications));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(CanSave));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private int? _modifierIdleTimeoutMs;

    public bool IsDirty => !_originalSnapshot.Equals(CreateCurrentSnapshot());

    public bool HasNoApplications => Applications.Count == 0;

    public bool HasValidationErrors => Applications.Any(a => a.HasValidationError);

    public bool CanSave => IsDirty && !HasValidationErrors;

    public string? ValidationSummary =>
        HasValidationErrors
            ? string.Join(Environment.NewLine, Applications
                .Where(a => a.ValidationError is not null)
                .Select(a => a.ValidationError)
                .Distinct())
            : null;

    protected SettingsViewModel()
    {
    }

    public SettingsViewModel(ConfigurationManager configurationManager, IconExtractor iconExtractor, ISnackbarService snackbarService)
    {
        _configurationManager = configurationManager;
        _iconExtractor = iconExtractor;
        _snackbarService = snackbarService;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        var config = _configurationManager.GetConfiguration()!;
        ModifierKey = config.Modifier;
        ModifierIdleTimeoutMs = config.ModifierIdleTimeoutMs;

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
        _dirtyTracker = new SettingsViewModelDirtyTracker(this, OnSettingsChanged);
        RunValidation();
    }

    private void OnSettingsChanged()
    {
        OnPropertyChanged(nameof(IsDirty));
        RunValidation();
    }

    private void RunValidation()
    {
        var errors = _validator.Validate(Applications);

        // Clear all existing error state
        foreach (var app in Applications)
            app.ValidationError = null;

        // Apply new errors to affected VMs
        foreach (var error in errors)
        {
            foreach (var app in error.AffectedApps)
                app.ValidationError = error.Message;
        }

        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ValidationSummary));
    }

    private SettingsSnapshot CreateCurrentSnapshot()
    {
        return new SettingsSnapshot(ModifierKey,
            Applications.Select(app =>
                    new ApplicationShortcutSnapshot(app.Key, app.ProcessName, app.StartIfNotRunning, app.CycleMode))
                .ToList());
    }

    public IEnumerable<string> BoundProcessNames => Applications.Select(a => a.ProcessName);

    public event Action<ApplicationShortcutViewModel>? ApplicationAdded;

    [RelayCommand]
    private void AddApplication(string processName)
    {
        var defaultIcon =
            _iconExtractor.GetByProcessName(
                Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\shell32.dll");

        var viewModel = new ApplicationShortcutViewModel
        {
            Key = Key.None,
            Name = processName,
            ProcessName = Path.GetFileName(processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName
                : processName + ".exe"),
            ProcessIcon = _iconExtractor.GetByProcessName(processName) ?? defaultIcon
        };

        Applications.Add(viewModel);
        ApplicationAdded?.Invoke(viewModel);
    }

    [RelayCommand]
    private void RemoveApplication(ApplicationShortcutViewModel application)
    {
        Applications.Remove(application);
    }

    [RelayCommand]
    private void SaveAndClose()
    {
        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in SaveAndClose");
#endif
            var newConfig = new Configuration.Configuration(
                ModifierIdleTimeoutMs,
                ModifierKey,
                Applications.Select(app =>
                    new ApplicationConfiguration(
                        app.Key,
                        app.ProcessName,
                        app.CycleMode,
                        app.StartIfNotRunning)).ToList());

            if (_configurationManager.UpdateConfiguration(newConfig))
            {
                _originalSnapshot = CreateCurrentSnapshot();
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(CanSave));

                _snackbarService.Show(
                    "Settings saved",
                    "Your changes have been applied.",
                    ControlAppearance.Success,
                    new SymbolIcon { Symbol = SymbolRegular.Checkmark20 },
                    TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception)
        {
            _snackbarService.Show(
                "Failed to save settings",
                "An error occurred while saving. Please try again.",
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle20 },
                TimeSpan.FromSeconds(5));
        }
    }

    [RelayCommand]
    private void Reset()
    {
        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in Reset");
#endif
            LoadConfiguration();
        }
        catch (Exception)
        {
            _snackbarService.Show(
                "Failed to reset settings",
                "Could not reload the configuration.",
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle20 },
                TimeSpan.FromSeconds(5));
        }
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationError))]
    private string? _validationError;

    public bool HasValidationError => ValidationError is not null;

    public ImageSource? ProcessIcon { get; init; }
}
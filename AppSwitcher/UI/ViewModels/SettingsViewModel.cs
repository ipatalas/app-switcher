using AppSwitcher.Configuration;
using AppSwitcher.Input;
using AppSwitcher.Startup;
using AppSwitcher.WindowDiscovery;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.ViewModels;

internal partial class SettingsViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan SnackbarTimeoutShort = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SnackbarTimeoutLong = TimeSpan.FromSeconds(5);

    private readonly AutoStart _autoStart = null!;
    private readonly ConfigurationManager _configurationManager = null!;
    private readonly IconExtractor _iconExtractor = null!;
    private readonly ISnackbarService _snackbarService = null!;
    private readonly IPackagedAppsService _packagedAppsService = null!;
    private readonly ApplicationsValidator _validator = null!;
    private readonly DynamicModeService _dynamicModeService = null!;
    private readonly IWindowEnumerator _windowEnumerator = null!;

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

        if (DynamicModeEnabled)
        {
            LoadDynamicApps();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private bool _pulseBorderEnabled = true;

    // see https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute -> DWMWA_BORDER_COLOR
    public static bool IsPulseBorderSupported { get; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private AppThemeSetting _theme = AppThemeSetting.System;

    public static IReadOnlyList<ThemeOption> AvailableThemes { get; } =
    [
        new(AppThemeSetting.System, "System (follow Windows)"),
        new(AppThemeSetting.Dark, "Dark"),
        new(AppThemeSetting.Light, "Light"),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private bool _overlayEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private int _overlayShowDelayMs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private bool _overlayKeepOpenWhileModifierHeld;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private bool _peekEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private bool _dynamicModeEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDynamicApplications))]
    private ObservableCollection<DynamicApplicationViewModel> _dynamicApplications = [];

    partial void OnDynamicApplicationsChanged(
        ObservableCollection<DynamicApplicationViewModel>? oldValue,
        ObservableCollection<DynamicApplicationViewModel> newValue)
    {
        if (oldValue is not null)
        {
            oldValue.CollectionChanged -= OnDynamicApplicationsCollectionChanged;
        }

        newValue.CollectionChanged += OnDynamicApplicationsCollectionChanged;
    }

    private void OnDynamicApplicationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasDynamicApplications));
    }

    partial void OnDynamicModeEnabledChanged(bool value)
    {
        if (value)
        {
            LoadDynamicApps();
        }
        else
        {
            DynamicApplications.Clear();
        }
    }

    public bool HasDynamicApplications => DynamicApplications.Count > 0;

    [ObservableProperty]
    private bool _launchAtStartup;

    partial void OnLaunchAtStartupChanged(bool value)
    {
        if (value)
        {
            _autoStart.CreateShortcut();
        }
        else
        {
            _autoStart.RemoveShortcut();
        }
    }

    public bool IsDirty => !_originalSnapshot.Equals(CreateCurrentSnapshot());

    public bool HasNoApplications => Applications.Count == 0;

    public bool HasValidationErrors => Applications.Any(a => a.HasValidationError);

    public bool CanSave => IsDirty && !HasValidationErrors;

    public string? ValidationSummary { get; private set; }

    protected SettingsViewModel()
    {
    }

    public SettingsViewModel(AutoStart autoStart, ConfigurationManager configurationManager,
        IconExtractor iconExtractor, ISnackbarService snackbarService, IPackagedAppsService packagedAppsService,
        ApplicationsValidator validator, DynamicModeService dynamicModeService, IWindowEnumerator windowEnumerator)
    {
        _autoStart = autoStart;
        _configurationManager = configurationManager;
        _iconExtractor = iconExtractor;
        _snackbarService = snackbarService;
        _packagedAppsService = packagedAppsService;
        _launchAtStartup = autoStart.IsEnabled();
        _validator = validator;
        _dynamicModeService = dynamicModeService;
        _windowEnumerator = windowEnumerator;
        LoadConfiguration();
    }

    public void LoadConfiguration()
    {
        var config = _configurationManager.GetConfiguration()!;
        ModifierKey = config.Modifier;
        PulseBorderEnabled = config.PulseBorderEnabled;
        Theme = config.Theme;
        OverlayEnabled = config.OverlayEnabled;
        OverlayShowDelayMs = config.OverlayShowDelayMs;
        OverlayKeepOpenWhileModifierHeld = config.OverlayKeepOpenWhileModifierHeld;
        PeekEnabled = config.PeekEnabled;
        DynamicModeEnabled = config.DynamicModeEnabled;

        _dirtyTracker?.Dispose();

        Applications = new ObservableCollection<ApplicationShortcutViewModel>(config.Applications.Select(app =>
        {
            var isPackaged = app.Type == ApplicationType.Packaged;
            var packagedApp = isPackaged
                ? _packagedAppsService.GetByAumid(app.Aumid)
                : null;

            var processIcon = isPackaged && packagedApp != null
                ? _iconExtractor.GetByIconPath(packagedApp.IconPath)
                : _iconExtractor.GetByProcessPath(app.ProcessPath);

            return new ApplicationShortcutViewModel
            {
                Key = app.Key,
                ProcessName = app.ProcessName,
                ProcessPath = app.ProcessPath,
                StartIfNotRunning = app.StartIfNotRunning,
                CycleMode = app.CycleMode,
                Type = app.Type,
                Aumid = app.Aumid,
                ProcessIcon = processIcon ?? _iconExtractor.GetDefaultIcon()
            };
        }).ToList());

        _originalSnapshot = CreateCurrentSnapshot();
        _dirtyTracker = new SettingsViewModelDirtyTracker(this, OnSettingsChanged);
        RunValidation();

        if (DynamicModeEnabled)
        {
            LoadDynamicApps();
        }
    }

    private void LoadDynamicApps()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_dynamicModeService is null || _windowEnumerator is null)
        {
            // it can be null in design-time
            return;
        }
        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

        var windows = _windowEnumerator.GetWindows();
        var dynamicConfigs = _dynamicModeService.GetAllDynamicApps(
            Applications.Select(a => new ApplicationConfiguration(a.Key, a.ProcessPath, a.CycleMode, a.StartIfNotRunning, a.Type, a.Aumid)).ToList(),
            windows);

        DynamicApplications = new ObservableCollection<DynamicApplicationViewModel>(
            dynamicConfigs.Select(cfg =>
            {
                var icon = _iconExtractor.GetByProcessPath(cfg.ProcessPath);
                return new DynamicApplicationViewModel
                {
                    Key = cfg.Key,
                    ProcessName = cfg.ProcessName,
                    ProcessPath = cfg.ProcessPath,
                    Type = cfg.Type,
                    ProcessIcon = icon
                };
            }));
    }

    private void OnSettingsChanged(string propertyName)
    {
        OnPropertyChanged(nameof(IsDirty));
        RunValidation();

        if (DynamicModeEnabled && propertyName == $"{nameof(Applications)}.{nameof(ApplicationShortcutViewModel.Key)}")
        {
            LoadDynamicApps();
        }
    }

    private void RunValidation()
    {
        var errors = _validator.Validate(Applications);

        foreach (var app in Applications)
        {
            app.ClearErrors();
        }

        foreach (var error in errors)
        {
            foreach (var app in error.AffectedApps)
            {
                app.AddError(error.Message);
            }
        }

        ValidationSummary = errors.Count switch
        {
            > 1 => "More than 1 error found...",
            1 => errors[0].Message,
            _ => null
        };

        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ValidationSummary));
    }

    private SettingsSnapshot CreateCurrentSnapshot()
    {
        return new SettingsSnapshot(ModifierKey,
            Applications.Select(app =>
                    new ApplicationShortcutSnapshot(app.Key, app.ProcessPath, app.StartIfNotRunning, app.CycleMode, app.Type, app.Aumid))
                .ToList(),
            PulseBorderEnabled,
            Theme,
            OverlayEnabled,
            OverlayShowDelayMs,
            OverlayKeepOpenWhileModifierHeld,
            PeekEnabled,
            DynamicModeEnabled);
    }

    public IEnumerable<string> BoundProcessNames => Applications.Select(a => a.ProcessName);

    public event Action<ApplicationShortcutViewModel>? ApplicationAdded;

    [RelayCommand]
    private void AddApplication(ApplicationSelectionArgs args)
    {
        if (Applications.Any(a => a.ProcessName.Equals(args.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            _snackbarService.Show(
                "Application already added",
                $"The application \"{args.ProcessName}\" is already in the list.",
                ControlAppearance.Caution,
                new SymbolIcon { Symbol = SymbolRegular.Warning20 },
                SnackbarTimeoutShort);
            return;
        }

        var packagedApp = args.Type == ApplicationType.Packaged
            ? _packagedAppsService.GetByInstalledPath(args.ProcessPath, args.ProcessId)
            : null;

        var processIcon = args.Type == ApplicationType.Packaged
            ? _iconExtractor.GetByIconPath(packagedApp!.IconPath)
            : _iconExtractor.GetByProcessPath(args.ProcessPath);

        var viewModel = new ApplicationShortcutViewModel
        {
            Key = (Key)(-1),
            ProcessPath = args.ProcessPath,
            ProcessName = args.ProcessName,
            Type = args.Type,
            Aumid = packagedApp?.Aumid,
            ProcessIcon = processIcon ?? _iconExtractor.GetDefaultIcon(),
            StartIfNotRunning = true
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
    private void PinApplication(DynamicApplicationViewModel dynamic)
    {
        if (Applications.Any(a => a.ProcessName.Equals(dynamic.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var icon = _iconExtractor.GetByProcessPath(dynamic.ProcessPath);

        var viewModel = new ApplicationShortcutViewModel
        {
            Key = dynamic.Key,
            ProcessPath = dynamic.ProcessPath,
            ProcessName = dynamic.ProcessName,
            Type = dynamic.Type,
            ProcessIcon = icon ?? _iconExtractor.GetDefaultIcon(),
            StartIfNotRunning = true
        };

        Applications.Add(viewModel);
        DynamicApplications.Remove(dynamic);
        ApplicationAdded?.Invoke(viewModel);
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
                ModifierKey,
                Applications.Select(app =>
                    new ApplicationConfiguration(
                        app.Key,
                        app.ProcessPath,
                        app.CycleMode,
                        app.StartIfNotRunning,
                        app.Type,
                        app.Aumid)).ToList(),
                PulseBorderEnabled,
                Theme,
                OverlayEnabled,
                OverlayShowDelayMs,
                OverlayKeepOpenWhileModifierHeld,
                PeekEnabled,
                DynamicModeEnabled);

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
                    SnackbarTimeoutShort);
            }
        }
        catch (Exception)
        {
            _snackbarService.Show(
                "Failed to save settings",
                "An error occurred while saving. Please try again.",
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle20 },
                SnackbarTimeoutLong);
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
                SnackbarTimeoutLong);
        }
    }

    public void Dispose()
    {
        _dirtyTracker?.Dispose();
    }
}

internal partial class ApplicationShortcutViewModel : ObservableObject, IApplicationConfiguration
{
    [ObservableProperty] private Key _key;

    [ObservableProperty] private string _processName = null!;
    [ObservableProperty] private string _processPath = null!;
    [ObservableProperty] private bool _startIfNotRunning;
    [ObservableProperty] private CycleMode _cycleMode = CycleMode.NextWindow;
    [ObservableProperty] private ApplicationType _type = ApplicationType.Win32;
    [ObservableProperty] private string? _aumid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationError))]
    private string? _validationError;

    public bool HasValidationError => ValidationError is not null;

    public ImageSource? ProcessIcon { get; init; }

    public void AddError(string error)
    {
        ValidationError = ValidationError is null ? error : $"• {ValidationError}{Environment.NewLine}• {error}".Trim();

        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(ValidationError));
    }

    public void ClearErrors()
    {
        ValidationError = null;

        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(ValidationError));
    }
}

internal record ThemeOption(AppThemeSetting Value, string DisplayName);

internal class DynamicApplicationViewModel
{
    public Key Key { get; init; }
    public string KeyLetter => Key.ToString();
    public string ProcessName { get; init; } = null!;
    public string ProcessPath { get; init; } = null!;
    public ApplicationType Type { get; init; } = ApplicationType.Win32;
    public ImageSource? ProcessIcon { get; init; }
}
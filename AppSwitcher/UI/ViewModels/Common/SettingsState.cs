using AppSwitcher.Configuration;
using AppSwitcher.Input;
using AppSwitcher.Startup;
using AppSwitcher.Stats;
using AppSwitcher.WindowDiscovery;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels.Common;

internal partial class SettingsState : ObservableObject, ISettingsState, IDisposable
{
    private readonly AutoStart _autoStart;
    private readonly ConfigurationManager _configurationManager;
    private readonly IconExtractor _iconExtractor;
    private readonly IPackagedAppsService _packagedAppsService;
    private readonly ILogger<SettingsState> _logger;
    private readonly ApplicationsValidator _validator;
    private readonly DynamicModeService _dynamicModeService;
    private readonly IWindowEnumerator _windowEnumerator;
    private readonly AppRegistryCache _appRegistryCache;

    private SettingsStateDirtyTracker? _dirtyTracker;
    private SettingsSnapshot _originalSnapshot = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private Key _modifierKey = Key.Apps;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private AppThemeSetting _theme = AppThemeSetting.System;

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
    [NotifyPropertyChangedFor(nameof(IsDirty), nameof(CanSave))]
    private bool _statsEnabled;

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

    public SettingsState(AutoStart autoStart, ConfigurationManager configurationManager,
        IconExtractor iconExtractor, IPackagedAppsService packagedAppsService, ILogger<SettingsState> logger,
        ApplicationsValidator validator, DynamicModeService dynamicModeService, IWindowEnumerator windowEnumerator,
        AppRegistryCache appRegistryCache)
    {
        _autoStart = autoStart;
        _configurationManager = configurationManager;
        _iconExtractor = iconExtractor;
        _packagedAppsService = packagedAppsService;
        _logger = logger;
        _launchAtStartup = autoStart.IsEnabled();
        _validator = validator;
        _dynamicModeService = dynamicModeService;
        _windowEnumerator = windowEnumerator;
        _appRegistryCache = appRegistryCache;
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
        StatsEnabled = config.StatsEnabled;

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
                DisplayName = _appRegistryCache.GetDisplayName(app.ProcessName),
                StartIfNotRunning = app.StartIfNotRunning,
                CycleMode = app.CycleMode,
                Type = app.Type,
                Aumid = app.Aumid,
                ProcessIcon = processIcon ?? _iconExtractor.GetDefaultIcon()
            };
        }).ToList());

        _originalSnapshot = CreateCurrentSnapshot();
        _dirtyTracker = new SettingsStateDirtyTracker(this, OnSettingsChanged);
        RunValidation();

        if (DynamicModeEnabled)
        {
            LoadDynamicApps();
        }
    }

    private void LoadDynamicApps()
    {
        var windows = _windowEnumerator.GetWindows();
        var dynamicConfigs = _dynamicModeService.GetAllDynamicApps(
            Applications.Select(a => new ApplicationConfiguration(a.Key, a.ProcessPath, a.CycleMode, a.StartIfNotRunning, a.Type, a.Aumid)).ToList(),
            windows);

        var imagePathToProcessId = windows.DistinctBy(w => w.ProcessImagePath)
            .ToDictionary(w => w.ProcessImagePath, w => w.ProcessId);

        DynamicApplications = new ObservableCollection<DynamicApplicationViewModel>(
            dynamicConfigs.Select(cfg =>
            {
                var icon = _iconExtractor.GetByProcessPath(cfg.ProcessPath);
                return new DynamicApplicationViewModel
                {
                    Key = cfg.Key,
                    ProcessName = cfg.ProcessName,
                    ProcessPath = cfg.ProcessPath,
                    DisplayName = _appRegistryCache.GetDisplayName(cfg.ProcessName, cfg.ProcessPath),
                    ProcessId = imagePathToProcessId.TryGetValue(cfg.ProcessPath, out var pid) ? pid : null,
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
            DynamicModeEnabled,
            StatsEnabled);
    }

    public ApplicationShortcutViewModel? AddApplication(ApplicationSelectionArgs args)
    {
        if (Applications.Any(a => a.ProcessName.Equals(args.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
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
            DisplayName = _appRegistryCache.GetDisplayName(args.ProcessName),
            Type = args.Type,
            Aumid = packagedApp?.Aumid,
            ProcessIcon = processIcon ?? _iconExtractor.GetDefaultIcon(),
            StartIfNotRunning = true
        };

        Applications.Add(viewModel);
        return viewModel;
    }

    public void RemoveApplication(ApplicationShortcutViewModel application)
    {
        Applications.Remove(application);
    }

    public ApplicationShortcutViewModel? PinApplication(DynamicApplicationViewModel dynamic)
    {
        if (Applications.Any(a => a.ProcessName.Equals(dynamic.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var packagedApp = dynamic.Type == ApplicationType.Packaged
            ? _packagedAppsService.GetByInstalledPath(dynamic.ProcessPath, dynamic.ProcessId)
            : null;

        var processIcon = dynamic.Type == ApplicationType.Packaged
            ? _iconExtractor.GetByIconPath(packagedApp!.IconPath)
            : _iconExtractor.GetByProcessPath(dynamic.ProcessPath);

        var viewModel = new ApplicationShortcutViewModel
        {
            Key = dynamic.Key,
            ProcessPath = dynamic.ProcessPath,
            ProcessName = dynamic.ProcessName,
            DisplayName = _appRegistryCache.GetDisplayName(dynamic.ProcessName),
            Type = dynamic.Type,
            Aumid = packagedApp?.Aumid,
            ProcessIcon = processIcon ?? _iconExtractor.GetDefaultIcon(),
            StartIfNotRunning = true
        };

        Applications.Add(viewModel);
        DynamicApplications.Remove(dynamic);
        return viewModel;
    }

    public bool Save()
    {
        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in Save");
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
                DynamicModeEnabled,
                StatsEnabled);

            if (_configurationManager.UpdateConfiguration(newConfig))
            {
                _originalSnapshot = CreateCurrentSnapshot();
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(CanSave));

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving settings");
        }

        return false;
    }

    public void Dispose()
    {
        _dirtyTracker?.Dispose();
    }
}
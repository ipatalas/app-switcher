using AppSwitcher.Configuration;
using AppSwitcher.UI.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AppSwitcher.UI.ViewModels.DesignTime;

internal class SettingsStateDesignTime : ObservableObject, ISettingsState
{
    public SettingsStateDesignTime()
    {
        var defaultIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/default_app_icon.png"));

        ModifierKey = Key.Apps;
        PulseBorderEnabled = true;
        Theme = AppThemeSetting.System;
        DynamicModeEnabled = false;
        LaunchAtStartup = true;
        PeekEnabled = true;
        StatsEnabled = true;
        OverlayEnabled = true;
        OverlayKeepOpenWhileModifierHeld = true;
        OverlayShowDelayMs = 300;
        Applications =
        [
            new()
            {
                Key = Key.C,
                ProcessName = "Code.exe",
                StartIfNotRunning = true,
                CycleMode = CycleMode.NextWindow,
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.Q,
                ProcessName = "qw.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.NextApp,
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.T,
                ProcessName = "WindowsTerminal.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Hide,
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.D,
                ProcessName = "DevToys.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Hide,
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.E,
                ProcessName = "brave.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.NextApp,
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.O,
                ProcessName = "obsidian.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Hide,
                ProcessIcon = defaultIcon
            }
        ];

        DynamicApplications =
        [
            new()
            {
                Key = Key.S,
                ProcessName = "Slack.exe",
                ProcessPath = "C:\\Program Files\\Slack\\Slack.exe",
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.N,
                ProcessName = "notepad.exe",
                ProcessPath = "C:\\Windows\\System32\\notepad.exe",
                ProcessIcon = defaultIcon,
            },
            new()
            {
                Key = Key.F,
                ProcessName = "firefox.exe",
                ProcessPath = "C:\\Program Files\\Mozilla Firefox\\firefox.exe",
                ProcessIcon = defaultIcon,
            }
        ];
    }

    public Key ModifierKey { get; set; }
    public ObservableCollection<ApplicationShortcutViewModel> Applications { get; set; }
    public bool PulseBorderEnabled { get; set; }
    public AppThemeSetting Theme { get; set; }
    public bool OverlayEnabled { get; set; }
    public int OverlayShowDelayMs { get; set; }
    public bool OverlayKeepOpenWhileModifierHeld { get; set; }
    public bool PeekEnabled { get; set; }
    public bool DynamicModeEnabled { get; set; }
    public bool StatsEnabled { get; set; }
    public bool HasNoApplications => Applications.Count == 0;
    public ObservableCollection<DynamicApplicationViewModel> DynamicApplications { get; set; }
    public bool HasDynamicApplications => DynamicApplications.Count > 0;
    public bool LaunchAtStartup { get; set; }

    public bool IsDirty => false;
    public bool CanSave => false;
    public bool HasValidationErrors => false;
    public string? ValidationSummary => null;

    public void LoadConfiguration() { }
    public bool Save() => true;
    public ApplicationShortcutViewModel? AddApplication(ApplicationSelectionArgs args) => null;
    public void RemoveApplication(ApplicationShortcutViewModel application) { }
    public ApplicationShortcutViewModel? PinApplication(DynamicApplicationViewModel dynamic) => null;
}
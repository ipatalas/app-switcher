using AppSwitcher.Configuration;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AppSwitcher.UI.ViewModels.DesignTime;

internal class SettingsViewModelDesignTime : SettingsViewModel
{
    public SettingsViewModelDesignTime()
    {
        var defaultIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/default_app_icon.png"));

        ModifierKey = Key.Apps;
        PulseBorderEnabled = true;
        Theme = AppThemeSetting.System;
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
    }
}
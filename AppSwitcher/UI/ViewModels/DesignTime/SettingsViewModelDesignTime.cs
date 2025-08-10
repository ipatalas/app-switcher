using AppSwitcher.Configuration;
using AppSwitcher.Utils;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels.DesignTime;

internal class SettingsViewModelDesignTime : SettingsViewModel
{
    public SettingsViewModelDesignTime()
    {
        var iconExtractor = new IconExtractor();
        ModifierKey = Key.Apps;

        Applications =
        [
            new()
            {
                Key = Key.C,
                ProcessName = "Code.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.NextWindow,
                ProcessIcon = iconExtractor.GetByProcessName("code.exe")
            },

            new()
            {
                Key = Key.Q,
                ProcessName = "qw.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Default,
                ProcessIcon = iconExtractor.GetByProcessName("qw.exe")
            },

            new()
            {
                Key = Key.T,
                ProcessName = "WindowsTerminal.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Hide,
                ProcessIcon = iconExtractor.GetByProcessName("WindowsTerminal.exe")
            },

            new()
            {
                Key = Key.D,
                ProcessName = "DevToys.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Hide,
                ProcessIcon = iconExtractor.GetByProcessName("DevToys.exe")
            },

            new()
            {
                Key = Key.E,
                ProcessName = "brave.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Default,
                ProcessIcon = iconExtractor.GetByProcessName("brave.exe")
            },

            new()
            {
                Key = Key.O,
                ProcessName = "obsidian.exe",
                StartIfNotRunning = false,
                CycleMode = CycleMode.Hide,
                ProcessIcon = iconExtractor.GetByProcessName("obsidian.exe")
            }
        ];
    }
}


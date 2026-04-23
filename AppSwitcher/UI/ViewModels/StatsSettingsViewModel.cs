using AppSwitcher.UI.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AppSwitcher.UI.ViewModels;

internal class StatsSettingsViewModel(ISettingsState state) : ObservableObject
{
    public ISettingsState State { get; } = state;
}
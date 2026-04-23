using AppSwitcher.UI.ViewModels.Common;

namespace AppSwitcher.UI.ViewModels;

internal class OverlaySettingsViewModel(ISettingsState state)
{
    public ISettingsState State { get; } = state;
}
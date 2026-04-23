using AppSwitcher.Extensions;
using AppSwitcher.UI.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.ViewModels;

internal partial class SettingsViewModel(ISettingsState state, ISnackbarService snackbarService) : ObservableObject
{
    public ISettingsState State { get; } = state;

    [RelayCommand]
    private void SaveAndClose()
    {
        if (State.Save())
        {
            snackbarService.ShowShort(
                "Settings saved",
                "Your changes have been applied.",
                ControlAppearance.Success,
                SymbolRegular.Checkmark20);
        }
        else
        {
            snackbarService.ShowLong(
                "Failed to save settings",
                "An error occurred while saving. Please try again.",
                ControlAppearance.Danger,
                SymbolRegular.ErrorCircle20);
        }
    }

    [RelayCommand]
    private void Reset() => State.LoadConfiguration();
}
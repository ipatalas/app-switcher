using AppSwitcher.Extensions;
using AppSwitcher.UI.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.ViewModels;

internal partial class HotkeysViewModel(ISettingsState state, ISnackbarService snackbarService) : ObservableObject
{
    public ISettingsState State { get; } = state;

    public event Action<ApplicationShortcutViewModel>? ApplicationAdded;

    [RelayCommand]
    private void AddApplication(ApplicationSelectionArgs args)
    {
        var vm = State.AddApplication(args);
        if (vm is not null)
        {
            ApplicationAdded?.Invoke(vm);
        }
        else
        {
            snackbarService.ShowShort(
                "Application already added",
                $"The application \"{args.ProcessName}\" is already in the list.",
                ControlAppearance.Caution,
                SymbolRegular.Warning20);
        }
    }

    [RelayCommand]
    private void RemoveApplication(ApplicationShortcutViewModel application) =>
        State.RemoveApplication(application);

    [RelayCommand]
    private void PinApplication(DynamicApplicationViewModel dynamic)
    {
        var vm = State.PinApplication(dynamic);
        if (vm is not null)
        {
            ApplicationAdded?.Invoke(vm);
        }
    }
}
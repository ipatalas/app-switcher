using AppSwitcher.Extensions;
using AppSwitcher.UI.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Wpf.Ui;
using Wpf.Ui.Controls;
using System.Windows;

namespace AppSwitcher.UI.ViewModels;

internal partial class HotkeysViewModel(ISettingsState state, ISnackbarService snackbarService) : ObservableObject, IDropTarget
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

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is ApplicationShortcutViewModel)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not ApplicationShortcutViewModel item)
        {
            return;
        }

        var oldIndex = State.Applications.IndexOf(item);
        var newIndex = dropInfo.InsertIndex;

        if (oldIndex < newIndex)
        {
            newIndex--;
        }

        if (oldIndex >= 0 && oldIndex != newIndex)
        {
            State.MoveApplication(oldIndex, newIndex);
        }
    }
}

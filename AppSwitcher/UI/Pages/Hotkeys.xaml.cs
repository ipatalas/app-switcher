using AppSwitcher.Extensions;
using AppSwitcher.UI.Controls;
using AppSwitcher.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class Hotkeys : Page
{
    private readonly SettingsViewModel _viewModel;

    public static readonly DependencyProperty FlyoutViewModelProperty =
        DependencyProperty.Register(
            nameof(FlyoutViewModel),
            typeof(AddApplicationFlyoutViewModel),
            typeof(Hotkeys),
            new PropertyMetadata());

    public AddApplicationFlyoutViewModel FlyoutViewModel
    {
        get => (AddApplicationFlyoutViewModel)GetValue(FlyoutViewModelProperty);
        private init => SetValue(FlyoutViewModelProperty, value);
    }

    public Hotkeys(SettingsViewModel viewModel, AddApplicationFlyoutViewModel flyoutViewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        FlyoutViewModel = flyoutViewModel;

        FlyoutViewModel.ApplicationSelected += OnApplicationSelected;
        _viewModel.ApplicationAdded += OnApplicationAdded;
        AddApplicationFlyout.CloseRequested += () => AddFlyoutPopup.IsOpen = false;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        FlyoutViewModel.Refresh(_viewModel.BoundProcessNames);
        AddFlyoutPopup.IsOpen = true;
    }

    private void AddFlyoutPopup_Opened(object sender, EventArgs e)
    {
        AddApplicationFlyout.FocusSearch();
    }

    private void OnApplicationSelected(string processName)
    {
        AddFlyoutPopup.IsOpen = false;
        _viewModel.AddApplicationCommand.Execute(processName);
    }

    private void OnApplicationAdded(ApplicationShortcutViewModel addedVm)
    {
        // Scroll to the new item and activate its key assignment button
        ApplicationsList.ScrollIntoView(addedVm);
        ApplicationsList.UpdateLayout();
        ActivateKeyAssignmentFor(addedVm);
    }

    private void ActivateKeyAssignmentFor(ApplicationShortcutViewModel targetVm)
    {
        if (ApplicationsList.ItemContainerGenerator.ContainerFromItem(targetVm) is not FrameworkElement container)
        {
            return;
        }

        var keyButton = container.FindVisualChild<KeyAssignmentButton>();
        keyButton?.StartListening();
    }
}

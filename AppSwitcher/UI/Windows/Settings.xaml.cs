using AppSwitcher.Extensions;
using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using System.ComponentModel;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.Windows;

internal partial class Settings
{
    private readonly SettingsViewModel _viewModel;

    public Settings(INavigationViewPageProvider pageProvider, SettingsViewModel viewModel, ISnackbarService snackbarService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        NavigationView.SetPageProviderService(pageProvider);
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);
    }

    protected override void OnActivated(EventArgs e)
    {
        if (NavigationView.SelectedItem is null)
        {
            NavigationView.Navigate(typeof(Hotkeys), _viewModel);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_viewModel.IsDirty)
        {
            var result = new MessageBox
            {
                Title = "Unsaved Changes",
                Content = "You have unsaved changes. Closing this window will discard them. Are you sure?",
                PrimaryButtonText = "Discard",
                PrimaryButtonAppearance = ControlAppearance.Secondary,
                SecondaryButtonText = "Cancel",
                SecondaryButtonAppearance = ControlAppearance.Primary,
                IsCloseButtonEnabled = false
            }.ShowSync();

            if (result != MessageBoxResult.Primary)
            {
                e.Cancel = true;
            }
        }
    }
}
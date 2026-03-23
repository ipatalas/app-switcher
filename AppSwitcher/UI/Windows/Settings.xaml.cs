using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

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
        // refresh every time Settings window is open
        // this will also fetch fresh icon for packaged apps (in case they were updated in the meantime)
        _viewModel.LoadConfiguration();

        if (NavigationView.SelectedItem is null)
        {
            NavigationView.Navigate(typeof(Hotkeys), _viewModel);
        }
    }
}
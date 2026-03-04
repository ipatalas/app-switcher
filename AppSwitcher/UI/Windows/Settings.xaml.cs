using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;
using Wpf.Ui.Abstractions;

namespace AppSwitcher.UI.Windows;

internal partial class Settings
{
    private readonly SettingsViewModel _viewModel;

    public Settings(INavigationViewPageProvider pageProvider, SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        NavigationView.SetPageProviderService(pageProvider);
    }

    protected override void OnActivated(EventArgs e)
    {
        if (NavigationView.SelectedItem is null)
        {
            NavigationView.Navigate(typeof(Hotkeys), _viewModel);
        }
    }
}
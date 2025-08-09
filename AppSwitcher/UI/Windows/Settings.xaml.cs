using AppSwitcher.UI.Pages;
using AppSwitcher.UI.ViewModels;

namespace AppSwitcher.UI.Windows;

public partial class Settings
{
    private readonly SettingsViewModel _viewModel;

    public Settings(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    protected override void OnActivated(EventArgs e)
    {
        NavigationView.Navigate(typeof(Hotkeys), _viewModel);
    }
}
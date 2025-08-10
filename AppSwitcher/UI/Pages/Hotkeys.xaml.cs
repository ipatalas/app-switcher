using AppSwitcher.UI.ViewModels;
using System.Windows.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class Hotkeys : Page
{
    public Hotkeys(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
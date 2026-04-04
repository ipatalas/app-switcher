using AppSwitcher.UI.ViewModels;
using System.Windows.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class General : Page
{
    public General(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
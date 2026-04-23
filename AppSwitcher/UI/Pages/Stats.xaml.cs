using AppSwitcher.UI.ViewModels;
using System.Windows.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class Stats : Page
{
    public Stats(StatsSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

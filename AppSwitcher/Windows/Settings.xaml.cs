using System.Windows;
using AppSwitcher.ViewModels;

namespace AppSwitcher.Windows;

public partial class Settings : Window
{
    public Settings(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
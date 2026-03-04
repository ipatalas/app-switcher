using AppSwitcher.UI.ViewModels;
using System.Windows.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class About : Page
{
    public About(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

using AppSwitcher.UI.ViewModels;
using System.Windows;
using Application = System.Windows.Application;

namespace AppSwitcher.UI.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal partial class MainWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // temporary workaround for WPF ContextMenu not binding DataContext correctly
    private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        ((System.Windows.Controls.ContextMenu)sender).DataContext = DataContext;
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void NotifyIcon_OnLeftDoubleClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
    {
        ((MainWindowViewModel)DataContext).OpenSettingsCommand.Execute(null);
    }
}
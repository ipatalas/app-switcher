using AppSwitcher.UI.ViewModels;
using System.Windows;

namespace AppSwitcher.UI.Windows;

public partial class AppOverlayWindow : Window
{
    public AppOverlayWindow(AppOverlayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => PositionWindow();
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => PositionWindow();

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = (workArea.Width - ActualWidth) / 2 + workArea.Left;
        Top = workArea.Bottom - ActualHeight - 20;
    }
}

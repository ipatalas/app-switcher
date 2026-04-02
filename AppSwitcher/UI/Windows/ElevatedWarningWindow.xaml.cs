using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Wpf.Ui.Appearance;

namespace AppSwitcher.UI.Windows;

public partial class ElevatedWarningWindow : Window
{
    public event EventHandler? DismissRequested;

    public ElevatedWarningWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Closed += OnClosed;

        ApplicationThemeManager.Changed += OnThemeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyShadowForTheme();
        PositionWindow();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => PositionWindow();

    private void OnClosed(object? sender, EventArgs e) =>
        ApplicationThemeManager.Changed -= OnThemeChanged;

    private void OnThemeChanged(ApplicationTheme theme, System.Windows.Media.Color _) => ApplyShadowForTheme();

    private void OnDismissClick(object sender, RoutedEventArgs e) =>
        DismissRequested?.Invoke(this, EventArgs.Empty);

    private void ApplyShadowForTheme()
    {
        var isDark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

        CardBorder.Effect = isDark
            ? new DropShadowEffect { BlurRadius = 20, ShadowDepth = 0, Opacity = 0.25, Color = Colors.White }
            : new DropShadowEffect { BlurRadius = 24, ShadowDepth = 8, Direction = 270, Opacity = 0.4, Color = Colors.Black };
    }

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - ActualHeight - 20;
    }
}
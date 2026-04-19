using AppSwitcher.Overlay;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Wpf.Ui.Appearance;

namespace AppSwitcher.UI.Windows;

public partial class WarningOverlayWindow : Window
{
    private Storyboard? _countdownStoryboard;

    public event EventHandler? DismissRequested;

    public WarningOverlayWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Closed += OnClosed;

        ApplicationThemeManager.Changed += OnThemeChanged;
    }

    internal void Configure(WarningContent content)
    {
        TitleText.Text = content.Title;
        MessageText.Text = content.Message;
        IconSymbol.Symbol = content.Icon;

        if (content.LearnMoreUrl is not null)
        {
            LearnMoreLink.NavigateUri = content.LearnMoreUrl;
            LearnMoreLink.Visibility = Visibility.Visible;
        }
        else
        {
            LearnMoreLink.Visibility = Visibility.Collapsed;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyShadowForTheme();
        UpdateContentClip();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateContentClip();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ApplicationThemeManager.Changed -= OnThemeChanged;
        _countdownStoryboard?.Stop(CountdownBar);
    }

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

    private void UpdateContentClip()
    {
        CardContent.Clip = new RectangleGeometry(
            new Rect(0, 0, CardContent.ActualWidth, CardContent.ActualHeight),
            radiusX: 12,
            radiusY: 12);
    }

    public void StartCountdown(double durationMilliseconds)
    {
        _countdownStoryboard?.Stop(CountdownBar);

        var animation = new DoubleAnimation
        {
            From = CardBorder.Width,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
            FillBehavior = FillBehavior.HoldEnd,
        };

        Storyboard.SetTarget(animation, CountdownBar);
        Storyboard.SetTargetProperty(animation, new PropertyPath(WidthProperty));

        _countdownStoryboard = new Storyboard();
        _countdownStoryboard.Children.Add(animation);
        _countdownStoryboard.Begin(CountdownBar, isControllable: true);
    }

    public void PauseCountdown() => _countdownStoryboard?.Pause(CountdownBar);
}
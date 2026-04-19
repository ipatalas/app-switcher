using AppSwitcher.UI.Windows;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;
using System.Windows;

namespace AppSwitcher.Overlay;

internal class WarningOverlayService : IDisposable
{
    private const int AutoDismissMs = 5000;
    private const int MaxShowCount = 3;

    private readonly WarningOverlayWindow _window;
    private readonly ILogger<WarningOverlayService> _logger;
    private readonly Dictionary<WarningContent, int> _showCounters = [];
    private Timer? _dismissTimer;

    public WarningOverlayService(WarningOverlayWindow window, ILogger<WarningOverlayService> logger)
    {
        _window = window;
        _logger = logger;

        _window.MouseEnter += OnMouseEnter;
        _window.MouseLeave += OnMouseLeave;
        _window.DismissRequested += OnDismissRequested;
    }

    public void Show(WarningContent content)
    {
        _showCounters.TryGetValue(content, out var count);
        if (count >= MaxShowCount)
        {
            return;
        }

        _showCounters[content] = count + 1;

        ScheduleDismiss();

        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            _window.Configure(content);
            _window.Show();
        });
    }

    private void ScheduleDismiss(int autoDismissMs = AutoDismissMs)
    {
        _window.StartCountdown(autoDismissMs);
        _dismissTimer?.Dispose();
        _dismissTimer = new Timer(
            callback: _ =>
            {
                Application.Current.Dispatcher.BeginInvoke(() => _window.Hide());
            },
            state: null,
            dueTime: autoDismissMs,
            period: Timeout.Infinite);
    }

    private void CancelDismiss()
    {
        _dismissTimer?.Dispose();
        _dismissTimer = null;
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _logger.LogDebug("Warning overlay: mouse entered, dismiss paused");
        CancelDismiss();
        _window.PauseCountdown();
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _logger.LogDebug("Warning overlay: mouse left, dismiss rescheduled");
        ScheduleDismiss(2000);
    }

    private void OnDismissRequested(object? sender, EventArgs e)
    {
        _logger.LogDebug("Warning overlay manually dismissed");
        CancelDismiss();
        Application.Current.Dispatcher.BeginInvoke(() => _window.Hide());
    }

    public void Dispose()
    {
        _dismissTimer?.Dispose();
        _window.MouseEnter -= OnMouseEnter;
        _window.MouseLeave -= OnMouseLeave;
        _window.DismissRequested -= OnDismissRequested;
    }
}
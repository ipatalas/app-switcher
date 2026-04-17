using AppSwitcher.UI.Windows;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;
using System.Windows;

namespace AppSwitcher.Overlay;

internal class ElevatedWarningService : IDisposable
{
    private const int AutoDismissMs = 5000;
    private const int MaxShowCount = 3;

    private readonly ElevatedWarningWindow _window;
    private readonly ILogger<ElevatedWarningService> _logger;
    private Timer? _dismissTimer;
    private int _showCounter;

    public ElevatedWarningService(ElevatedWarningWindow window, ILogger<ElevatedWarningService> logger)
    {
        _window = window;
        _logger = logger;

        _window.MouseEnter += OnMouseEnter;
        _window.MouseLeave += OnMouseLeave;
        _window.DismissRequested += OnDismissRequested;
    }

    public void Show()
    {
        if (_showCounter++ >= MaxShowCount)
        {
            // only show it 3 times
            return;
        }

        ScheduleDismiss();

        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
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
        _logger.LogDebug("Elevated warning: mouse entered, dismiss paused");
        CancelDismiss();
        _window.PauseCountdown();
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _logger.LogDebug("Elevated warning: mouse left, dismiss rescheduled");
        ScheduleDismiss(2000);
    }

    private void OnDismissRequested(object? sender, EventArgs e)
    {
        _logger.LogDebug("Elevated warning manually dismissed");
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
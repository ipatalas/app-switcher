using Microsoft.Extensions.Logging;

namespace AppSwitcher.Overlay;

internal class OverlayShowTimer(ILogger<OverlayShowTimer> logger) : IDisposable
{
    private Timer? _timer;
    private int _timeoutMs;

    private Action? _onExpired;

    public void Configure(Action onExpired, int timeoutMs)
    {
        _onExpired = onExpired;
        _timeoutMs = timeoutMs;
        logger.LogDebug("Overlay show timer configured ({TimeoutMs}ms)", timeoutMs);
        Cancel();
    }

    /// <summary>
    /// Starts the one-shot timer. Subsequent calls while the timer is already running
    /// are no-ops — the timer is NOT restarted.
    /// </summary>
    public void Start()
    {
        if (_timer is not null)
        {
            logger.LogDebug("Overlay show timer already running — ignoring Start()");
            return;
        }

        _timer = new Timer(
            callback: _ =>
            {
                logger.LogDebug("Overlay show timer expired");
                _onExpired?.Invoke();
            },
            state: null,
            dueTime: _timeoutMs,
            period: Timeout.Infinite
        );

        logger.LogDebug("Overlay show timer started ({TimeoutMs}ms)", _timeoutMs);
    }

    public void Cancel()
    {
        if (_timer is not null)
        {
            _timer.Dispose();
            _timer = null;
            logger.LogDebug("Overlay show timer cancelled");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
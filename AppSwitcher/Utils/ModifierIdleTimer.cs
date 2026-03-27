using Microsoft.Extensions.Logging;

namespace AppSwitcher.Utils;

internal class ModifierIdleTimer(ILogger<ModifierIdleTimer> logger) : IDisposable
{
    private const int DefaultTimeoutMs = 0;

    private System.Threading.Timer? _timer;
    private int _timeoutMs = DefaultTimeoutMs;

    private Action? OnExpired { get; set; }

    private bool IsDisabled => _timeoutMs == 0;
    private bool IsDefault => _timeoutMs == DefaultTimeoutMs;

    public void Configure(Action onExpired, int? timeoutMs)
    {
        OnExpired = onExpired;
        _timeoutMs = timeoutMs ?? DefaultTimeoutMs;
        if (!IsDefault)
        {
            logger.LogDebug("Modifier idle timer timeout: {TimeoutMs}ms (0 means disabled)", timeoutMs);
        }
        Cancel();
    }

    public void Restart()
    {
        if (IsDisabled)
        {
            return;
        }

        _timer?.Dispose();

        _timer = new System.Threading.Timer(
            callback: _ =>
            {
                logger.LogDebug("Modifier idle timer expired");
                OnExpired?.Invoke();
            },
            state: null,
            dueTime: _timeoutMs,
            period: Timeout.Infinite
        );

        logger.LogDebug("Modifier idle timer (re)started ({TimeoutMs}ms)", _timeoutMs);
    }

    public void Cancel()
    {
        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
            logger.LogDebug("Modifier idle timer cancelled");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

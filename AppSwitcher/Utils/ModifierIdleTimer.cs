using Microsoft.Extensions.Logging;

namespace AppSwitcher.Utils;

internal class ModifierIdleTimer(ILogger<ModifierIdleTimer> logger) : IDisposable
{
    private System.Threading.Timer? _timer;
    private int _timeoutMs = 2000;

    public Action? OnExpired { get; set; }

    public int TimeoutMs
    {
        get => _timeoutMs;
        set
        {
            _timeoutMs = value;
            Cancel();
        }
    }

    private bool IsDisabled => _timeoutMs == 0;

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

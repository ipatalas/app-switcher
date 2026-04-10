using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.Input;

internal sealed record PeekResult(
    ApplicationWindow PreviousWindow,
    HWND TargetHandle,
    bool TargetWasMinimized);

internal class Peeker(ILogger<Peeker> logger) : IDisposable
{
    internal const int PeekThresholdMs = 400;

    private System.Threading.Timer? _timer;
    private ApplicationWindow? _previousWindow;
    private HWND _targetHandle;
    private bool _targetWasMinimized;
    private volatile bool _active; // written from ThreadPool timer callback, read from hook thread

    public void Arm(ApplicationWindow previousWindow, ApplicationWindow targetWindow)
    {
        Cancel();
        _previousWindow = previousWindow;
        _targetHandle = targetWindow.Handle;
        _targetWasMinimized = targetWindow.State == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED;
        _timer = new System.Threading.Timer(_ => Activate(), null, PeekThresholdMs, Timeout.Infinite);
        logger.LogDebug("Peek armed (threshold: {ThresholdMs}ms, target was minimized: {WasMinimized})",
            PeekThresholdMs, _targetWasMinimized);
    }

    private void Activate()
    {
        _active = true;
        logger.LogDebug("Peek mode active - waiting for key release");
    }

    public bool TryFinish([NotNullWhen(true)] out PeekResult? result)
    {
        result = _active
            ? new PeekResult(_previousWindow!, _targetHandle, _targetWasMinimized)
            : null;

        var wasActive = _active;
        Cancel();

        if (wasActive)
        {
            logger.LogDebug("Peek finished - restoring previous window");
        }

        return wasActive;
    }

    public void Cancel()
    {
        _timer?.Dispose();
        _timer = null;
        _active = false;
        _previousWindow = null;
        _targetHandle = default;
        _targetWasMinimized = false;
    }

    public void Dispose() => Cancel();
}
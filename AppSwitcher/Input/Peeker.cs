using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.Input;

internal sealed record PeekResult(
    ApplicationWindow PreviousWindow,
    HWND TargetHandle,
    bool TargetWasMinimized,
    string TargetProcessName,
    string TargetProcessPath,
    long ArmedAtTick,
    bool IsDynamic);

internal class Peeker(ILogger<Peeker> logger) : IDisposable
{
    internal const int PeekThresholdMs = 400;

    private Timer? _timer;
    private ApplicationWindow? _previousWindow;
    private HWND _targetHandle;
    private bool _targetWasMinimized;
    private string _targetProcessName = "";
    private string _targetProcessPath = "";
    private long _armedAtTick;
    private bool _isDynamic;
    private volatile bool _active; // written from ThreadPool timer callback, read from hook thread

    public void Arm(ApplicationWindow previousWindow, AppSwitchResult result, bool isDynamic)
    {
        Cancel();
        _previousWindow = previousWindow;
        _targetHandle = result.Handle;
        _targetWasMinimized = result.State == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED;
        _targetProcessName = result.ProcessName;
        _targetProcessPath = result.ProcessPath;
        _armedAtTick = Environment.TickCount64;
        _isDynamic = isDynamic;
        _timer = new Timer(_ => Activate(), null, PeekThresholdMs, Timeout.Infinite);
        logger.LogDebug(
            "Peek armed (threshold: {ThresholdMs}ms, target was minimized: {WasMinimized}, previous window: {ProcessName}/{Handle})",
            PeekThresholdMs, _targetWasMinimized, previousWindow.ProcessName, previousWindow.Handle);
    }

    private void Activate()
    {
        _active = true;
        logger.LogDebug("Peek mode active - waiting for key release");
    }

    public bool TryFinish([NotNullWhen(true)] out PeekResult? result)
    {
        result = _active
            ? new PeekResult(_previousWindow!, _targetHandle, _targetWasMinimized, _targetProcessName, _targetProcessPath, _armedAtTick, _isDynamic)
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
        _targetProcessName = "";
        _armedAtTick = 0;
        _isDynamic = false;
    }

    public void Dispose() => Cancel();
}
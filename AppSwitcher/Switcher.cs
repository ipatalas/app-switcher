using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.Graphics.Dwm;

namespace AppSwitcher;

internal sealed record AppSwitchResult(
    uint? ProcessId,
    string ProcessPath,
    HWND Handle,
    SHOW_WINDOW_CMD State,
    bool NeedsElevation,
    bool WasStarted)
{
    public string ProcessName { get; } = Path.GetFileName(ProcessPath);

    public static AppSwitchResult FromApplicationWindow(ApplicationWindow w) =>
        new(w.ProcessId, w.ProcessImagePath, w.Handle, SHOW_WINDOW_CMD.SW_SHOW, w.NeedsElevation, WasStarted: false);

    public static AppSwitchResult FromStarted(string processPath) =>
        new(null, processPath, HWND.Null, SHOW_WINDOW_CMD.SW_SHOW, NeedsElevation: false, WasStarted: true);
};

internal class Switcher(ILogger<Switcher> logger, IWindowEnumerator windowEnumerator, ConfigurationManager configurationManager)
{
    // Avoid re-launching slow-starting apps when the hotkey is pressed repeatedly
    // while the first instance is still creating its window.
    private const int StartDebounceMs = 10_000;

    private readonly List<HWND> _nextWindows = [];
    private readonly Dictionary<string, HWND> _lastToggleWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _recentStarts = new(StringComparer.OrdinalIgnoreCase);

    public AppSwitchResult? Execute(IReadOnlyList<ApplicationConfiguration> appGroup)
    {
        if (appGroup.Count == 1)
        {
            return Execute(appGroup[0]);
        }

        var currentWindow = windowEnumerator.GetCurrentWindow();
        var currentIndex = appGroup
            .Select((app, i) => new { app, i })
            .FirstOrDefault(x => currentWindow?.ProcessImagePath.EndsWith(x.app.ProcessName, StringComparison.CurrentCultureIgnoreCase) == true)
            ?.i ?? -1;

        var nextApp = appGroup[(currentIndex + 1) % appGroup.Count];
        logger.LogDebug("Cycling app group: current index {CurrentIndex}, next app {NextApp}", currentIndex, nextApp.ProcessName);
        return Execute(nextApp);
    }

    public bool SwitchToWindowByIndex(
        IReadOnlyList<ApplicationConfiguration> applications,
        int index,
        out ApplicationWindow? focusedWindow)
    {
        focusedWindow = null;
        var allWindows = windowEnumerator.GetWindows();
        var focusedAppWindows = windowEnumerator.GetFocusedAppWindows(allWindows, applications);
        var focusedAppWindow = focusedAppWindows.FocusedWindow!;
        if (index < 0 || index >= focusedAppWindows.Count)
        {
            return false;
        }

        if (index == focusedAppWindows.FocusedWindowIndex)
        {
            // already focused, nothing to do but digit key press must be suppressed no to be sent to the active application
            return true;
        }

        var appName = focusedAppWindow.GetProductName() ?? Path.GetFileNameWithoutExtension(focusedAppWindow.ProcessImagePath);

        logger.LogDebug("Switching to window #{Number} of {AppName}", index + 1, appName);
        if (ActivateWindow(focusedAppWindows[index]))
        {
            focusedWindow = focusedAppWindows[index];
        }

        return true;
    }

    /// <summary>
    /// Switch to given application
    /// </summary>
    /// <param name="appConfig">configuration of target app</param>
    /// <returns>app window that was just focused (only if switching between apps)</returns>
    private AppSwitchResult? Execute(ApplicationConfiguration appConfig)
    {
        var topLevelWindows = windowEnumerator.GetWindows();
        var matchingWindows = topLevelWindows.Where(w =>
                w.ProcessImagePath.EndsWith(appConfig.ProcessName, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        if (matchingWindows.Count == 0)
        {
            if (appConfig.StartIfNotRunning)
            {
                if (WasStartedRecently(appConfig))
                {
                    logger.LogInformation(
                        "{ProcessName} was started less than {DebounceSeconds}s ago and has no window yet - ignoring repeated start request",
                        appConfig.ProcessName, StartDebounceMs / 1000);
                    return null;
                }

                if (appConfig.Type == ApplicationType.Packaged)
                {
                    Process.Start("explorer.exe", $"shell:AppsFolder\\{appConfig.Aumid}");
                    logger.LogInformation("Starting packaged app {ProcessName} via AUMID {Aumid}",
                        appConfig.ProcessName, appConfig.Aumid);
                }
                else
                {
                    Process.Start(appConfig.ProcessPath);
                    logger.LogInformation("Starting {ProcessName}", appConfig.ProcessName);
                }

                _recentStarts[appConfig.ProcessPath] = Stopwatch.GetTimestamp();
                return AppSwitchResult.FromStarted(appConfig.ProcessPath);
            }
            logger.LogWarning("{ProcessName} process not found", appConfig.ProcessName);
            return null;
        }

        var currentWindow = windowEnumerator.GetCurrentWindow();
        var firstWindow = matchingWindows[0];

        // ToggleWindow has its own remembered-target logic; other modes share the off-app gate.
        if (appConfig.CycleMode == CycleMode.ToggleWindow)
        {
            return ExecuteToggleWindow(appConfig, matchingWindows, currentWindow);
        }

        var isOnThisApp = currentWindow is not null &&
                          matchingWindows.Exists(w => w.ProcessId == currentWindow.ProcessId);

        if (!isOnThisApp || appConfig.CycleMode == CycleMode.NextApp)
        {
            return ActivateFirst(appConfig, firstWindow);
        }

        if (appConfig.CycleMode == CycleMode.Hide)
        {
            logger.LogInformation("Hiding {ProcessName} window {Handle} (state {State})",
                appConfig.ProcessName, firstWindow.Handle, firstWindow.State);
            HideWindow(firstWindow.Handle);
            return null;
        }

        if (appConfig.CycleMode == CycleMode.NextWindow)
        {
            var nextWindow = GetNextWindow(matchingWindows, currentWindow!);
            logger.LogInformation("Switching to next window of {ProcessName}: {Handle} (state {State})",
                appConfig.ProcessName, nextWindow.Handle, nextWindow.State);
            ActivateWindow(nextWindow);
            return null;
        }

        return ActivateFirst(appConfig, firstWindow);
    }

    private AppSwitchResult? ActivateFirst(ApplicationConfiguration appConfig, ApplicationWindow window)
    {
        _nextWindows.Clear();
        logger.LogDebug("Switching to {ProcessName}", appConfig.ProcessName);
        return TryActivate(window);
    }

    private AppSwitchResult? ExecuteToggleWindow(
        ApplicationConfiguration appConfig,
        List<ApplicationWindow> matchingWindows,
        ApplicationWindow? currentWindow)
    {
        HWND? remembered = _lastToggleWindows.TryGetValue(appConfig.ProcessName, out var handle)
            ? handle
            : null;
        var rememberedWindow = remembered is { } rememberedHandle
            ? matchingWindows.Find(w => w.Handle == rememberedHandle)
            : null;

        var matchingHandles = matchingWindows.ConvertAll(static w => w.Handle);
        var resolution = ToggleWindowTargetResolver.Resolve(
            matchingHandles,
            currentWindow?.Handle,
            remembered,
            currentIsMinimized: IsMinimized(currentWindow),
            rememberedIsMinimized: IsMinimized(rememberedWindow));

        if (resolution is null)
        {
            return null;
        }

        var target = matchingWindows.Find(w => w.Handle == resolution.Value.Handle);
        if (target is null)
        {
            return null;
        }

        _lastToggleWindows[appConfig.ProcessName] = target.Handle;

        if (resolution.Value.Action == ToggleWindowAction.Hide)
        {
            logger.LogInformation("ToggleWindow: hiding {ProcessName} window {Handle} (state {State}, foreground is {Foreground})",
                appConfig.ProcessName, target.Handle, target.State, DescribeWindow(currentWindow));
            HideWindow(target.Handle);
            return null;
        }

        logger.LogInformation("ToggleWindow: activating {ProcessName} window {Handle} (state {State}, foreground is {Foreground})",
            appConfig.ProcessName, target.Handle, target.State, DescribeWindow(currentWindow));
        return TryActivate(target);
    }

    private ApplicationWindow GetNextWindow(List<ApplicationWindow> matchingWindows, ApplicationWindow window)
    {
        logger.LogTrace("Matching windows:");
        windowEnumerator.LogWindows(LogLevel.Trace, matchingWindows);

        if (!_nextWindows.Contains(window.Handle))
        {
            _nextWindows.Add(window.Handle);
        }
        logger.LogTrace("Next windows: {Handles}", string.Join(", ", _nextWindows));

        if (matchingWindows.TrueForAll(w => _nextWindows.Contains(w.Handle)))
        {
            var idx = _nextWindows.IndexOf(window.Handle);
            var nextHandle = _nextWindows[(idx + 1) % _nextWindows.Count];

            return matchingWindows.First(w => w.Handle == nextHandle);
        }

        return matchingWindows.First(w => !_nextWindows.Contains(w.Handle));
    }

    private AppSwitchResult? TryActivate(ApplicationWindow window) =>
        ActivateWindow(window) ? AppSwitchResult.FromApplicationWindow(window) : null;

    internal bool ActivateWindow(ApplicationWindow window, bool pulseBorder = true)
    {
        var hwnd = window.Handle;
        if (PInvoke.IsIconic(hwnd))
        {
            logger.LogDebug("Window is minimized - restoring...");
            if (window.NeedsElevation)
            {
                // SwitchToThisWindow works better than ShowWindow with SW_RESTORE when target window is elevated process
                PInvoke.SwitchToThisWindow(hwnd, true);
            }
            else
            {
                // be polite most of the time when SwitchToThisWindow is not needed
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
            }
        }

        // Hack for SetForegroundWindow limitations (see Remarks here: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow#remarks)
        // https://github.com/microsoft/PowerToys/blob/8b8c75b9a54a342660ce8475989ef283fe39e8fe/src/common/ManagedCommon/WindowHelpers.cs#L11
        INPUT input = new() { type = INPUT_TYPE.INPUT_MOUSE, Anonymous = new() { mi = new() } };
        INPUT[] inputs = [input];
        PInvoke.SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));

        var setForegroundSucceeded = (bool)PInvoke.SetForegroundWindow(hwnd);

        if (PInvoke.GetForegroundWindow() != hwnd)
        {
            setForegroundSucceeded = ForceForegroundViaAttachThreadInput(hwnd);
        }

        if (PInvoke.GetForegroundWindow() != hwnd)
        {
            // Last resort: the mechanism used by the Alt+Tab switcher, not subject to foreground-lock
            // restrictions - needed e.g. when a ShellHost.exe light-dismiss flyout (Quick Settings)
            // holds the foreground.
            PInvoke.SwitchToThisWindow(hwnd, true);
        }

        var activated = PInvoke.GetForegroundWindow() == hwnd;
        if (!activated)
        {
            logger.LogWarning(
                "Failed to bring {ProcessName} window {Handle} to the foreground (SetForegroundWindow returned {SetForegroundResult}); {ForegroundInfo}",
                window.ProcessName, hwnd, setForegroundSucceeded, DescribeForegroundWindow());
        }

        if (activated && pulseBorder && configurationManager.GetConfiguration()?.PulseBorderEnabled == true)
        {
            // it needs to go async because we're calling it from a keyboard hook which has narrow time limits
            _ = PulseBorder(hwnd, Color.Gold, 100);
        }

        return activated;
    }

    // SetForegroundWindow is called on our own thread, so it is our thread that must share input
    // state with the foreground window's thread to pass the foreground-rights check (the previous
    // version attached the target and foreground threads to each other, which grants nothing).
    private static bool ForceForegroundViaAttachThreadInput(HWND hwnd)
    {
        var foregroundHandle = PInvoke.GetForegroundWindow();
        if (foregroundHandle == hwnd || foregroundHandle == HWND.Null)
        {
            return foregroundHandle == hwnd;
        }

        var currentThreadId = PInvoke.GetCurrentThreadId();
        var foregroundThreadId = GetWindowThreadProcessId(foregroundHandle);
        var targetThreadId = GetWindowThreadProcessId(hwnd);

        var attachedToForeground = foregroundThreadId != currentThreadId &&
                                   PInvoke.AttachThreadInput(currentThreadId, foregroundThreadId, true);
        var attachedToTarget = targetThreadId != currentThreadId &&
                               targetThreadId != foregroundThreadId &&
                               PInvoke.AttachThreadInput(currentThreadId, targetThreadId, true);
        try
        {
            var succeeded = (bool)PInvoke.SetForegroundWindow(hwnd);
            if (PInvoke.GetForegroundWindow() != hwnd)
            {
                PInvoke.BringWindowToTop(hwnd);
            }

            return succeeded;
        }
        finally
        {
            if (attachedToTarget)
            {
                PInvoke.AttachThreadInput(currentThreadId, targetThreadId, false);
            }
            if (attachedToForeground)
            {
                PInvoke.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    private string DescribeForegroundWindow()
    {
        try
        {
            return DescribeWindow(windowEnumerator.GetCurrentWindow());
        }
        catch (Exception ex)
        {
            return $"unidentifiable: {ex.Message}";
        }
    }

    private static string DescribeWindow(ApplicationWindow? window) =>
        window is null
            ? "unknown"
            : $"{window.ProcessName} window {window.Handle} (\"{window.Title}\", state {window.State}, " +
              $"position ({window.Position.X},{window.Position.Y}), size {window.Size.Width}x{window.Size.Height})";

    private static bool IsMinimized(ApplicationWindow? window) =>
        window is
        {
            State: SHOW_WINDOW_CMD.SW_SHOWMINIMIZED
                or SHOW_WINDOW_CMD.SW_MINIMIZE
                or SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE
                or SHOW_WINDOW_CMD.SW_FORCEMINIMIZE
        };

    private async Task PulseBorder(IntPtr hwnd, Color highlightColor, int durationMs = 400)
    {
        var color = highlightColor.R | (highlightColor.G << 8) | (highlightColor.B << 16);
        // ReSharper disable once InconsistentNaming
        var DWMWA_COLOR_DEFAULT = -1;

        await Task.Run(async () =>
        {
            SetDwmColor(color);
            await Task.Delay(durationMs);
            SetDwmColor(DWMWA_COLOR_DEFAULT);
            await Task.Delay(durationMs);
            SetDwmColor(color);
            await Task.Delay(durationMs);
            SetDwmColor(DWMWA_COLOR_DEFAULT);
        });

        unsafe void SetDwmColor(int colorRef)
        {
            // The pointer is only "alive" for the duration of this specific call
            PInvoke.DwmSetWindowAttribute(
                new HWND(hwnd),
                DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR,
                &colorRef,
                sizeof(int)
            );
        }
    }

    internal void HideWindow(HWND hwnd)
    {
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE);
    }

    private bool WasStartedRecently(ApplicationConfiguration appConfig) =>
        _recentStarts.TryGetValue(appConfig.ProcessPath, out var startedAtTick) &&
        Stopwatch.GetElapsedTime(startedAtTick).TotalMilliseconds < StartDebounceMs;

    private static unsafe uint GetWindowThreadProcessId(HWND hwnd)
    {
        return PInvoke.GetWindowThreadProcessId(hwnd, null);
    }
}
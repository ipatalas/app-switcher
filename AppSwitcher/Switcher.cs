using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AppSwitcher;

internal class Switcher(ILogger<Switcher> logger, WindowHelper windowHelper)
{
    private readonly List<HWND> _nextWindows = [];

    public void Execute(IReadOnlyList<ApplicationConfiguration> appGroup)
    {
        if (appGroup.Count == 1)
        {
            Execute(appGroup[0]);
            return;
        }

        var currentWindow = windowHelper.GetCurrentWindow();
        var currentIndex = appGroup
            .Select((app, i) => new { app, i })
            .FirstOrDefault(x => currentWindow?.ProcessImageName.EndsWith(x.app.ProcessName, StringComparison.CurrentCultureIgnoreCase) == true)
            ?.i ?? -1;

        var nextApp = appGroup[(currentIndex + 1) % appGroup.Count];
        logger.LogDebug("Cycling app group: current index {CurrentIndex}, next app {NextApp}", currentIndex, nextApp.NormalizedProcessName);
        Execute(nextApp);
    }

    private void Execute(ApplicationConfiguration appConfig)
    {
        var topLevelWindows = windowHelper.GetWindows();
        var matchingWindows = topLevelWindows.Where(w =>
                w.ProcessImageName.EndsWith(appConfig.ProcessName, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        var window = matchingWindows.FirstOrDefault();
        if (window is null)
        {
            if (appConfig.StartIfNotRunning)
            {
                Process.Start(appConfig.Process);
                logger.LogInformation("Starting {ProcessName}", appConfig.NormalizedProcessName);
                return;
            }
            logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
            return;
        }

        var currentWindow = windowHelper.GetCurrentWindow();

        if (currentWindow?.ProcessId != window.ProcessId || appConfig.CycleMode == CycleMode.NextApp)
        {
            _nextWindows.Clear();
            logger.LogDebug("Switching to {ProcessName}", appConfig.NormalizedProcessName);
            ActivateWindow(window);
        }
        else if (appConfig.CycleMode == CycleMode.Hide)
        {
            logger.LogDebug("Hiding {ProcessName}", appConfig.NormalizedProcessName);
            HideWindow(window);
        }
        else if (appConfig.CycleMode == CycleMode.NextWindow)
        {
            logger.LogDebug("Switching to next window of {ProcessName}", appConfig.NormalizedProcessName);
            var nextWindow = GetNextWindow(matchingWindows, currentWindow);
            logger.LogDebug("Selected next window: {Handle}", nextWindow.Handle);
            ActivateWindow(nextWindow);
        }
    }

    private ApplicationWindow GetNextWindow(List<ApplicationWindow> matchingWindows, ApplicationWindow window)
    {
        logger.LogTrace("Matching windows:");
        windowHelper.LogWindows(LogLevel.Trace, matchingWindows);

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

    private void ActivateWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        if (PInvoke.IsIconic(hwnd))
        {
            logger.LogDebug("Window is minimized - restoring...");
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        }

        // Hack for SetForegroundWindow limitations (see Remarks here: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow#remarks)
        // https://github.com/microsoft/PowerToys/blob/8b8c75b9a54a342660ce8475989ef283fe39e8fe/src/common/ManagedCommon/WindowHelpers.cs#L11
        INPUT input = new() { type = INPUT_TYPE.INPUT_MOUSE, Anonymous = new() { mi = new() } };
        INPUT[] inputs = [input];
        PInvoke.SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));

        PInvoke.SetForegroundWindow(hwnd);

        var fgHandle = PInvoke.GetForegroundWindow();

        if (fgHandle != hwnd)
        {
            var threadId1 = GetWindowThreadProcessId(hwnd);
            var threadId2 = GetWindowThreadProcessId(fgHandle);

            if (threadId1 != threadId2)
            {
                PInvoke.AttachThreadInput(threadId1, threadId2, true);
                PInvoke.SetForegroundWindow(hwnd);
                PInvoke.AttachThreadInput(threadId1, threadId2, false);
            }
            else
            {
                PInvoke.SetForegroundWindow(hwnd);
            }
        }
    }

    private void HideWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE);
    }

    private static unsafe uint GetWindowThreadProcessId(HWND hwnd)
    {
        unsafe
        {
            return PInvoke.GetWindowThreadProcessId(hwnd, null);
        }
    }
}

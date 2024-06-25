using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32;
using System.Diagnostics;

namespace AppSwitcher;

internal class Switcher
{
    private readonly ILogger<Switcher> _logger;
    private readonly WindowHelper _windowHelper;
    private readonly List<HWND> _nextWindows = new();

    public Switcher(ILogger<Switcher> logger, WindowHelper windowHelper)
    {
        _logger = logger;
        _windowHelper = windowHelper;
    }

    public void Execute(ApplicationConfiguration appConfig)
    {
        var topLevelWindows = _windowHelper.GetWindows();
        var matchingWindows = topLevelWindows.Where(w => w.ProcessImageName.EndsWith(appConfig.ProcessName, StringComparison.CurrentCultureIgnoreCase));
        var window = matchingWindows.FirstOrDefault();
        if (window is null)
        {
            if (appConfig.StartIfNotRunning)
            {
                Process.Start(appConfig.Process);
                _logger.LogInformation("Starting {ProcessName}", appConfig.NormalizedProcessName);
                return;
            }
            _logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
            return;
        }

        var currentWindow = _windowHelper.GetCurrentWindow();

        if (currentWindow.ProcessId != window.ProcessId || appConfig.CycleMode == CycleMode.Default)
        {
            _nextWindows.Clear();
            _logger.LogDebug("Switching to {ProcessName}", appConfig.NormalizedProcessName);
            ActivateWindow(window);
        }
        else if (appConfig.CycleMode == CycleMode.Hide)
        {
            _logger.LogDebug("Hiding {ProcessName}", appConfig.NormalizedProcessName);
            HideWindow(window);
        }
        else if (appConfig.CycleMode == CycleMode.NextWindow)
        {
            _logger.LogDebug("Switching to next window of {ProcessName}", appConfig.NormalizedProcessName);
            var nextWindow = GetNextWindow(matchingWindows.ToList(), currentWindow);
            _logger.LogDebug("Selected next window: {Handle}", nextWindow.Handle);
            ActivateWindow(nextWindow);
        }
    }

    private ApplicationWindow GetNextWindow(List<ApplicationWindow> matchingWindows, ApplicationWindow window)
    {
        _logger.LogTrace("Matching windows:");
        _windowHelper.LogWindows(matchingWindows);

        if (!_nextWindows.Contains(window.Handle))
        {
            _nextWindows.Add(window.Handle);
        }
        _logger.LogTrace("Next windows: {Handles}", string.Join(", ", _nextWindows));

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
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(hwnd);
    }

    private void HideWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE);
    }
}

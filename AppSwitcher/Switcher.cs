using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.Windows.Input;
using Win32 = Windows.Win32;


namespace AppSwitcher;
internal class Switcher
{
    private readonly ILogger<Switcher> _logger;
    private readonly WindowHelper _windowHelper;

    public Switcher(ILogger<Switcher> logger, WindowHelper windowHelper)
    {
        _logger = logger;
        _windowHelper = windowHelper;
    }

    public void Execute(ApplicationConfiguration appConfig)
    {
        var topLevelWindows = _windowHelper.GetWindows(true);
        var window = topLevelWindows.FirstOrDefault(w => w.ProcessImageName.EndsWith(appConfig.NormalizedProcessName, StringComparison.CurrentCultureIgnoreCase));
        if (window is null)
        {
            _logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
            return;
        }

        var currentWindow = _windowHelper.GetCurrentWindow();

        if (currentWindow.ProcessId != window.ProcessId || appConfig.CycleMode == CycleMode.Default)
        {
            _logger.LogDebug("Switching to {ProcessName}", appConfig.NormalizedProcessName);
            ActivateWindow(window);
        }
        else if (appConfig.CycleMode == CycleMode.Hide)
        {
            _logger.LogDebug("Hiding {ProcessName}", appConfig.NormalizedProcessName);
            HideWindow(window);
        }
    }

    private void ActivateWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        Win32.PInvoke.ShowWindow(hwnd, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
        Win32.PInvoke.SetForegroundWindow(hwnd);
    }

    private void HideWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        Win32.PInvoke.ShowWindow(hwnd, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MINIMIZE);
    }
}

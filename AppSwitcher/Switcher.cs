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

    public void Execute(Key modifier, Key letter, ApplicationConfiguration appConfig)
    {
        var topLevelWindows = _windowHelper.GetWindows(true);
        var window = topLevelWindows.FirstOrDefault(w => w.ProcessImageName.EndsWith(appConfig.NormalizedProcessName, StringComparison.CurrentCultureIgnoreCase));
        if (window is null)
        {
            _logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
            return;
        }

        _logger.LogDebug("{Modifier}-{Letter} pressed - switching to {ProcessName}", modifier, letter, appConfig.NormalizedProcessName);
        ActivateWindow(window);
    }

    private void ActivateWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        Win32.PInvoke.ShowWindow(hwnd, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
        Win32.PInvoke.SetForegroundWindow(hwnd);
    }
}

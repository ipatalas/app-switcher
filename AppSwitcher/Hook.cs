using AppSwitcher.WindowDiscovery;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

using Win32 = Windows.Win32;

namespace AppSwitcher;

internal class Hook : IDisposable
{
    private readonly KeyboardHook _hook;
    private readonly ILogger<Hook> _logger;
    private readonly WindowHelper _windowHelper;
    private readonly HashSet<Key> _keysDown = [];
    private bool _suppressKeyUpEvents = false;
    private Configuration.Configuration? _config;

    public Hook(ILogger<Hook> logger, WindowHelper windowHelper)
    {
        _hook = new KeyboardHook();
        this._logger = logger;
        this._windowHelper = windowHelper;
    }

    public void Start(Configuration.Configuration config)
    {
        this._config = config;
        _logger.LogInformation("Starting hook");
        _hook.KeyboardPressed += Hook_KeyboardPressed;
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping hook");
        _hook.KeyboardPressed -= Hook_KeyboardPressed;
    }

    public void Dispose()
    {
        Stop();
        _hook.Dispose();
    }

    private void Hook_KeyboardPressed(object? sender, KeyboardHookEventArgs e)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_config);

            if (!IsLetter(e.InputEvent.Key) && !IsModifier(e.InputEvent.Key))
            {
                return;
            }

            HandleKeyPress(e, _config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling key press");
        }
    }

    private void HandleKeyPress(KeyboardHookEventArgs e, Configuration.Configuration config)
    {
        var isKeyDownEvent = e.KeyPressType is KeyboardHook.KeyPressType.KeyDown or KeyboardHook.KeyPressType.SysKeyDown;

        if (isKeyDownEvent)
        {
            _keysDown.Add(e.InputEvent.Key);

            if (_keysDown.Count == 2 && _keysDown.Contains(config.Modifier))
            {
                _keysDown.Remove(config.Modifier);
                var letter = _keysDown.Single();

                var appConfig = config.Applications.FirstOrDefault(a => a.Key == letter);
                if (appConfig is not null)
                {
                    e.SuppressKeyPress = true;
                    _suppressKeyUpEvents = true;

                    var topLevelWindows = _windowHelper.GetWindows(true);
                    var window = topLevelWindows.FirstOrDefault(w => w.ProcessImageName.EndsWith(appConfig.NormalizedProcessName, StringComparison.CurrentCultureIgnoreCase));
                    if (window is null)
                    {
                        _logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
                        return;
                    }

                    _logger.LogDebug("{Modifier}-{Letter} pressed - switching to {ProcessName}", config.Modifier, letter, appConfig.NormalizedProcessName);
                    ActivateWindow(window);
                }
            }
        }
        else
        {
            _keysDown.Remove(e.InputEvent.Key);
            if (e.InputEvent.Key == config.Modifier && _suppressKeyUpEvents)
            {
                // When releasing the modifier just after the application switch, passing this event down the line to target app can cause side effects.
                // For instance for Apps modifier it would open up context menu in the target app right after the switch.
                e.SuppressKeyPress = true;
                _suppressKeyUpEvents = false;
                _logger.LogDebug("Suppressing {Key} release", e.InputEvent.Key);
            }
        }
    }

    private static void ActivateWindow(ApplicationWindow window)
    {
        var hwnd = window.Handle;
        Win32.PInvoke.ShowWindow(hwnd, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
        Win32.PInvoke.SetForegroundWindow(hwnd);
    }

    private bool IsLetter(Key key) => key is >= Key.A and <= Key.Z;
    private bool IsModifier(Key key) => _config!.Modifier == key;
}

using AppSwitcher.WindowDiscovery;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

using Win32 = Windows.Win32;

namespace AppSwitcher;

internal class Hook : IDisposable
{
    private readonly KeyboardHook hook;
    private readonly ILogger<Hook> logger;
    private readonly WindowHelper windowHelper;
    private readonly HashSet<Key> keysDown = [];
    private bool suppressKeyUpEvents = false;
    private Configuration.Configuration? config;

    public Hook(ILogger<Hook> logger, WindowHelper windowHelper)
    {
        hook = new KeyboardHook();
        this.logger = logger;
        this.windowHelper = windowHelper;
    }

    public void Start(Configuration.Configuration config)
    {
        this.config = config;
        logger.LogInformation("Starting hook");
        hook.KeyboardPressed += Hook_KeyboardPressed;
    }

    public void Stop()
    {
        logger.LogInformation("Stopping hook");
        hook.KeyboardPressed -= Hook_KeyboardPressed;
    }

    public void Dispose()
    {
        Stop();
        hook.Dispose();
    }

    private void Hook_KeyboardPressed(object? sender, KeyboardHookEventArgs e)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(config);

            if (!IsLetter(e.InputEvent.Key) && !IsModifier(e.InputEvent.Key))
            {
                return;
            }

            HandleKeyPress(e, config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling key press");
        }
    }

    private void HandleKeyPress(KeyboardHookEventArgs e, Configuration.Configuration config)
    {
        var isKeyDownEvent = e.KeyPressType is KeyboardHook.KeyPressType.KeyDown or KeyboardHook.KeyPressType.SysKeyDown;

        if (isKeyDownEvent)
        {
            keysDown.Add(e.InputEvent.Key);

            if (keysDown.Count == 2 && keysDown.Contains(config.Modifier))
            {
                keysDown.Remove(config.Modifier);
                var letter = keysDown.Single();

                var appConfig = config.Applications.FirstOrDefault(a => a.Key == letter);
                if (appConfig is not null)
                {
                    e.SuppressKeyPress = true;
                    suppressKeyUpEvents = true;

                    var topLevelWindows = windowHelper.GetWindows(true);
                    var window = topLevelWindows.FirstOrDefault(w => w.Process.FileName.EndsWith(appConfig.NormalizedProcessName, StringComparison.CurrentCultureIgnoreCase));
                    if (window is null)
                    {
                        logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
                        return;
                    }

                    logger.LogDebug("{Modifier}-{Letter} pressed - switching to {ProcessName}", config.Modifier, letter, appConfig.NormalizedProcessName);
                    ActivateWindow(window);
                }
            }
        }
        else
        {
            keysDown.Remove(e.InputEvent.Key);
            if (e.InputEvent.Key == config.Modifier && suppressKeyUpEvents)
            {
                // When releasing the modifier just after the application switch, passing this event down the line to target app can cause side effects.
                // For instance for Apps modifier it would open up context menu in the target app right after the switch.
                e.SuppressKeyPress = true;
                suppressKeyUpEvents = false;
                logger.LogDebug("Suppressing {Key} release", e.InputEvent.Key);
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
    private bool IsModifier(Key key) => config!.Modifier == key;
}

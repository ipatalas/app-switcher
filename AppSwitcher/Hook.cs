using AppSwitcher.WindowDiscovery;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

using Win32 = Windows.Win32;

namespace AppSwitcher
{
    internal class Hook : IDisposable
    {
        private readonly KeyboardHook hook;
        private readonly ILogger<Hook> logger;
        private readonly WindowHelper windowHelper;
        private readonly Key selectedModifier = Key.Apps;
        private bool suppressKeyUpEvents = false;

        public Hook(ILogger<Hook> logger, WindowHelper windowHelper)
        {
            hook = new KeyboardHook();
            this.logger = logger;
            this.windowHelper = windowHelper;
        }

        public void Start()
        {
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
            if (!IsLetter(e.InputEvent.Key) && !IsModifier(e.InputEvent.Key))
            {
                return;
            }

            var isKeyDownEvent = e.KeyPressType is KeyboardHook.KeyPressType.KeyDown or KeyboardHook.KeyPressType.SysKeyDown;

            if (isKeyDownEvent)
            {
                keysDown.Add(e.InputEvent.Key);

                if (keysDown.Count == 2 && keysDown.Contains(selectedModifier))
                {
                    keysDown.Remove(selectedModifier);
                    var letter = keysDown.Single();

                    if (letterAppMap.TryGetValue(letter, out var filename))
                    {
                        e.SuppressKeyPress = true;
                        suppressKeyUpEvents = true;

                        var windows = windowHelper.GetWindows(true);
                        var window = windows.FirstOrDefault(w => w.Process.FileName.EndsWith(filename));
                        if (window is null)
                        {
                            logger.LogWarning("{ProcessName} process not found", filename);
                            return;
                        }

                        logger.LogDebug("{Modifier}-{Letter} pressed - switching to {ProcessName}", selectedModifier, letter, filename);
                        var hwnd = window.Handle;
                        Win32.PInvoke.ShowWindow(hwnd, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
                        Win32.PInvoke.SetForegroundWindow(hwnd);
                    }
                }
            }
            else
            {
                keysDown.Remove(e.InputEvent.Key);
                if (e.InputEvent.Key == selectedModifier && suppressKeyUpEvents)
                {
                    // When releasing the modifier just after the application switch, passing this event down the line to target app can cause side effects.
                    // For instance for Apps modifier it would open up context menu in the target app right after the switch.
                    e.SuppressKeyPress = true;
                    suppressKeyUpEvents = false;
                    logger.LogDebug("Suppressing {Key} release", e.InputEvent.Key);
                }
            }            
        }

        private IDictionary<Key, string> letterAppMap = new Dictionary<Key, string>
        {
            { Key.T, "WindowsTerminal.exe" },
            { Key.V, "devenv.exe" },
            { Key.E, "Brave.exe" },
            { Key.M, "MailClient.exe" },
            { Key.Q, "qw.exe" }
        };

        private HashSet<Key> keysDown = new();

        private readonly HashSet<Key> modifiers = new()
        {
            Key.LeftAlt, Key.LeftCtrl, Key.LWin, Key.RightAlt, Key.RightCtrl, Key.Apps
        };

        private bool IsLetter(Key key) => key >= Key.A && key <= Key.Z;
        private bool IsModifier(Key key) => modifiers.Contains(key);
    }
}

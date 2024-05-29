using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using Win32 = Windows.Win32;

namespace Avalonia_MVVM
{
    internal class Hook : IDisposable
    {
        private readonly KeyboardHook hook;
        private readonly ILogger<Hook> logger;
        private readonly WindowHelper windowHelper;
        private readonly Key selectedModifier = Key.Apps;

        public Hook(ILogger<Hook> logger, WindowHelper windowHelper)
        {
            this.hook = new KeyboardHook();
            this.logger = logger;
            this.windowHelper = windowHelper;
        }

        public void Start()
        {
            this.logger.LogInformation("Starting hook");
            this.hook.KeyboardPressed += Hook_KeyboardPressed;
        }

        public void Stop()
        {
            this.logger.LogInformation("Stopping hook");
            this.hook.KeyboardPressed -= Hook_KeyboardPressed;
        }

        public void Dispose()
        {
            this.Stop();
            this.hook.Dispose();
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
            }
            else
            {
                keysDown.Remove(e.InputEvent.Key);
                if (e.InputEvent.Key == selectedModifier)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (isKeyDownEvent && keysDown.Count == 2 && keysDown.Contains(selectedModifier))
            {
                keysDown.Remove(selectedModifier);
                var letter = keysDown.Single();

                if (letterAppMap.TryGetValue(letter, out var appProductName))
                {
                    var windows = windowHelper.GetWindows(true);
                    var window = windows.FirstOrDefault(w => w.Process.ProductName == appProductName);
                    if (window is null)
                    {
                        this.logger.LogWarning("{ProductName} process not found", appProductName);
                        return;
                    }

                    this.logger.LogInformation("App-{Letter} pressed - switching to {ProductName}", letter, appProductName);
                    var hwnd = window.Handle;
                    Win32.PInvoke.ShowWindow(hwnd, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
                    Win32.PInvoke.SetForegroundWindow(hwnd);
                    e.SuppressKeyPress = true;
                }
            }
        }

        private IDictionary<Key, string> letterAppMap = new Dictionary<Key, string>
        {
            { Key.T, "Windows Terminal" },
            { Key.V, "Microsoft® Visual Studio®" },
            { Key.E, "Brave Browser" },
            { Key.M, "eM Client" },
            { Key.Q, "Quicken for Windows" }
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

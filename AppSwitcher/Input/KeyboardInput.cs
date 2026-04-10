using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace AppSwitcher.Input;

internal static class KeyboardInput
{
    public static bool SendSyntheticKeyDownUp(Key key)
    {
        var virtualKey = (VIRTUAL_KEY)KeyInterop.VirtualKeyFromKey(key);

        KEYBD_EVENT_FLAGS downFlags = 0, upFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        if (IsExtendedKey(key))
        {
            downFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY;
            upFlags |= KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY;
        }

        INPUT[] inputs =
        [
            KeyEventInput(virtualKey, downFlags),
            KeyEventInput(virtualKey, upFlags)
        ];

        var result = PInvoke.SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));

        return result == inputs.Length;

        INPUT KeyEventInput(VIRTUAL_KEY vKey, KEYBD_EVENT_FLAGS flags)
        {
            return new()
            {
                type = INPUT_TYPE.INPUT_KEYBOARD,
                Anonymous = new()
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vKey,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = 0
                    }
                }
            };
        }
    }

    private static bool IsExtendedKey(Key key) => key is Key.RightCtrl or Key.RightAlt or Key.Apps;
}

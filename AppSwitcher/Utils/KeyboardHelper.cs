using System.Windows.Input;
using Windows.Win32;

namespace AppSwitcher.Utils;

internal static class KeyboardHelper
{
    public static bool IsKeyDown(Key key)
    {
        var vKey = KeyInterop.VirtualKeyFromKey(key);
        var state = PInvoke.GetAsyncKeyState(vKey);

        return (state & 0x8000) != 0;
    }
}

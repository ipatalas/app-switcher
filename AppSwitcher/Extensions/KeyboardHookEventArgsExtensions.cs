using KeyboardHookLite;

namespace AppSwitcher.Extensions;

public static class KeyboardHookEventArgsExtensions
{
    public static bool IsKeyDown(this KeyboardHookEventArgs e) =>
        e.KeyPressType is KeyboardHook.KeyPressType.KeyDown or KeyboardHook.KeyPressType.SysKeyDown;
}

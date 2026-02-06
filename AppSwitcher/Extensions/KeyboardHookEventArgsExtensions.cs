using KeyboardHookLite;

namespace AppSwitcher.Extensions;

public static class KeyboardHookEventArgsExtensions
{
    private const int LLKHF_INJECTED = 0x00000010;

    public static bool IsInjected(this KeyboardHookEventArgs e) =>
        (e.InputEvent.Flags & LLKHF_INJECTED) != 0;

    public static bool IsKeyDown(this KeyboardHookEventArgs e) =>
        e.KeyPressType is KeyboardHook.KeyPressType.KeyDown or KeyboardHook.KeyPressType.SysKeyDown;

    public static string ToFriendlyString(this KeyboardHookEventArgs e) =>
        $"{(e.IsKeyDown() ? "[↓]" : "[↑]")} {e.InputEvent.Key}";
}

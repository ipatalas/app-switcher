using KeyboardHookLite;

namespace AppSwitcher.Extensions;

public static class KeyboardHookEventArgsExtensions
{
    private const int LLKHF_INJECTED = 0x00000010;

    extension(KeyboardHookEventArgs e)
    {
        public bool IsInjected => (e.InputEvent.Flags & LLKHF_INJECTED) != 0;

        public bool IsKeyDown =>
            e.KeyPressType is KeyboardHook.KeyPressType.KeyDown or KeyboardHook.KeyPressType.SysKeyDown;

        public string ToFriendlyString() => $"{(e.IsKeyDown ? "[↓]" : "[↑]")} {e.InputEvent.Key}";
    }
}
namespace AppSwitcher.WindowDiscovery;

// https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
[Flags]
internal enum WindowStyleEx : uint
{
    WS_EX_ACCEPTFILES = 0x00000010,
    WS_EX_APPWINDOW = 0x00040000,
    WS_EX_CLIENTEDGE = 0x00000200,
    WS_EX_COMPOSITED = 0x02000000,
    WS_EX_CONTEXTHELP = 0x00000400,
    WS_EX_CONTROLPARENT = 0x00010000,
    WS_EX_DLGMODALFRAME = 0x00000001,
    WS_EX_LAYERED = 0x00080000,
    WS_EX_LAYOUTRTL = 0x00400000,
    WS_EX_LEFT = 0x00000000,
    WS_EX_LEFTSCROLLBAR = 0x00004000,
    WS_EX_LTRREADING = 0x00000000,
    WS_EX_MDICHILD = 0x00000040,
    WS_EX_NOACTIVATE = 0x08000000,
    WS_EX_NOINHERITLAYOUT = 0x00100000,
    WS_EX_NOPARENTNOTIFY = 0x00000004,
    WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
    WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),
    WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),
    WS_EX_RIGHT = 0x00001000,
    WS_EX_RIGHTSCROLLBAR = 0x00000000,
    WS_EX_RTLREADING = 0x00002000,
    WS_EX_STATICEDGE = 0x00020000,
    WS_EX_TOOLWINDOW = 0x00000080,
    WS_EX_TOPMOST = 0x00000008,
    WS_EX_TRANSPARENT = 0x00000020,
    WS_EX_WINDOWEDGE = 0x00000100,
}

internal class WindowStyleHelpers
{
    // Standard ToString() won't work well in this case because the enum above is not covering all window styles, hence ToString() will return the numeric value
    public static string GetString<T>(T style)
        where T : struct, Enum
    {
        var values = Enum.GetValues<T>();

        var result = new HashSet<string>();

        foreach (var v in values)
        {
            if (style.HasFlag(v))
            {
                result.Add(v.ToString());
            }
        }

        return string.Join(" | ", result);
    }
}
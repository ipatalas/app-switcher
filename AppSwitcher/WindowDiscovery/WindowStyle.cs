namespace AppSwitcher.WindowDiscovery;

[Flags]
internal enum WindowStyle : uint
{
    None = 0,
    WS_CHILD = 0x40000000,
    WS_POPUP = 0x80000000,
    // See https://learn.microsoft.com/en-us/windows/win32/winmsg/window-styles for more
}

internal class WindowStyleHelpers
{
    // Standard ToString() won't work well in this case because the enum above is not covering all window styles, hence ToString() will return the numeric value
    public static string GetString(WindowStyle style)
    {
        var values = Enum.GetValues<WindowStyle>();

        var result = new List<string>();

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
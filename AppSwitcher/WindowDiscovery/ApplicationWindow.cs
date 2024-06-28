using System.Diagnostics;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

[DebuggerDisplay("{Title} ({ProcessId})")]
internal record ApplicationWindow(HWND Handle,
                                  string Title,
                                  int ProcessId,
                                  string ProcessImageName,
                                  SHOW_WINDOW_CMD State,
                                  Point Position,
                                  Size Size,
                                  WINDOW_STYLE Style,
                                  WindowStyleEx StyleEx)
{
    public bool IsValidWindow =>
        Size != Size.Empty && !string.IsNullOrEmpty(Title) && !StyleEx.HasFlag(WindowStyleEx.WS_EX_NOACTIVATE);
}

using System.Diagnostics;
using System.Drawing;
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
                                  Size Size);

using System;
using System.Drawing;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Avalonia_MVVM
{
    internal record ApplicationWindow(HWND Handle,
                                      string Title,
                                      ApplicationWindowProcess Process,
                                      SHOW_WINDOW_CMD State,
                                      Point Position,
                                      Size Size);

    internal record ApplicationWindowProcess(int ProcessId,
                                             string Name,
                                             string ProductName,
                                             string FileName);
}

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

internal class WindowHelper
{
    private readonly ILogger<WindowHelper> logger;

    public WindowHelper(ILogger<WindowHelper> logger)
    {
        this.logger = logger;
    }

    public IList<ApplicationWindow> GetWindows(bool currentDesktop)
    {
        var sw = Stopwatch.StartNew();
        var handles = GetVisibleWindowsHandles(currentDesktop);

        var result = handles.Select(handle =>
        {
            // TODO: use WinApi to get process info to see if that improves performance
            var hwnd = new HWND(handle);
            WINDOWPLACEMENT placement = new();
            PInvoke.GetWindowPlacement(hwnd, ref placement);
            var window = placement.rcNormalPosition;
            var title = GetWindowText(hwnd);
            GetWindowThreadProcessId(hwnd, out var processId);
            var process = Process.GetProcessById((int)processId);
            var mainModule = process.MainModule!;

            var appWinProcess = new ApplicationWindowProcess(process.Id, process.ProcessName, mainModule.FileVersionInfo.ProductName!, mainModule.FileName!);
            var position = new Point(window.left, window.top);
            var size = new Size(window.right - window.left, window.bottom - window.top);

            return new ApplicationWindow(hwnd, title, appWinProcess, placement.showCmd, position, size);
        })
        .Where(item => item.Size != Size.Empty && !string.IsNullOrEmpty(item.Title))
        .ToList();

        logger.LogDebug($"Found {result.Count} non-empty windows in {sw.ElapsedMilliseconds}ms ({sw.ElapsedTicks})");

        return result;
    }

    private ISet<nint> GetVisibleWindowsHandles(bool currentDesktop)
    {
        var sw = Stopwatch.StartNew();
        var result = new HashSet<nint>();

        var shellWindow = PInvoke.GetShellWindow();

        WNDENUMPROC enumerator = delegate (HWND hwnd, LPARAM lParam)
        {
            if (hwnd != shellWindow && PInvoke.IsWindowVisible(hwnd) && PInvoke.IsWindow(hwnd))
            {
                result.Add(hwnd);
            }

            return true;
        };

        if (currentDesktop)
        {
            var handle = PInvoke.GetThreadDesktop(PInvoke.GetCurrentThreadId());
            PInvoke.EnumDesktopWindows(handle, enumerator, nint.Zero);
        }
        else
        {
            PInvoke.EnumWindows(enumerator, nint.Zero);
        }

        logger.LogDebug($"Found {result.Count} windows in {sw.ElapsedMilliseconds}ms ({sw.ElapsedTicks})");

        return result;
    }

    internal unsafe static uint GetWindowThreadProcessId(HWND hwnd, out uint processId)
    {
        fixed (uint* lpdwProcessId = &processId)
        {
            return PInvoke.GetWindowThreadProcessId(hwnd, lpdwProcessId);
        }
    }

    internal unsafe static string GetWindowText(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);
        string s = new('_', length + 1);
        fixed (char* p = s)
        {
            PInvoke.GetWindowText(hwnd, p, length + 1);
        }

        return s;
    }
}

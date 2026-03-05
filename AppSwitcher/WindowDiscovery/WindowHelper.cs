using AppSwitcher.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

internal class WindowHelper(ILogger<WindowHelper> logger, ProcessPathExtractor processPathExtractor)
{
    public IList<ApplicationWindow> GetWindows()
    {
        var sw = Stopwatch.StartNew();
        var windows = GetVisibleWindowsHandles();

        var result = windows
            .Select(w => GetApplicationWindow(w.Handle, w.Style, w.StyleEx))
            .Where(item => item is { IsValidWindow: true })
            .Select(item => item!)
            .ToList();

        logger.LogTrace($"Found {result.Count} non-empty windows in {sw.ElapsedMilliseconds}ms");
        LogWindows(LogLevel.Trace, result);

        return result;
    }

    public ApplicationWindow? GetCurrentWindow()
    {
        var hwnd = PInvoke.GetForegroundWindow();

        var style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

        while (style.HasFlag(WINDOW_STYLE.WS_CHILD))
        {
            hwnd = PInvoke.GetParent(hwnd);
            style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        }

        var styleEx = (WindowStyleEx)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        return GetApplicationWindow(hwnd, style, styleEx);
    }

    public void LogWindows(LogLevel level, IEnumerable<ApplicationWindow> result)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        foreach (var item in result)
        {
            logger.Log(level,
                "PID/Handle: {ProcessId}/{Handle}, {ProcessName} ({ProductName}), {Title}, {State}, {WindowStyle}, {WindowStyleEx}, {IsCloaked}",
                item.ProcessId, item.Handle, item.ProcessImageName, item.GetProductName(), item.Title, item.State,
                WindowStyleHelpers.GetString(item.Style), WindowStyleHelpers.GetString(item.StyleEx),
                item.IsCloaked ? "Cloaked" : "Visible");
        }
    }

    public List<ApplicationWindow> GetAllWindows()
    {
        var sw = Stopwatch.StartNew();
        var windows = GetVisibleWindowsHandles();
        var result = windows
            .Select(w => GetApplicationWindow(w.Handle, w.Style, w.StyleEx))
            .Where(item => item != null)
            .Select(item => item!)
            .ToList();

        logger.LogInformation($"Found {result.Count} windows in {sw.ElapsedMilliseconds}ms");
        return result;
    }

    public void LogAllWindows()
    {
        var result = GetAllWindows();
        LogWindows(LogLevel.Information, result);
    }

    private unsafe ApplicationWindow? GetApplicationWindow(HWND hwnd, WINDOW_STYLE style, WindowStyleEx styleEx)
    {
        WINDOWPLACEMENT placement = new();
        PInvoke.GetWindowPlacement(hwnd, ref placement);
        var window = placement.rcNormalPosition;

        var title = GetWindowText(hwnd);
        GetWindowThreadProcessId(hwnd, out var processId);

        var position = new Point(window.left, window.top);
        var size = new Size(window.right - window.left, window.bottom - window.top);

        var processImageName = processPathExtractor.GetProcessImageName(processId);
        if (processImageName is null)
        {
            return null;
        }

        var cloaked = 0;
        var hresult = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(int));
        if (hresult != 0)
        {
            logger.LogWarning("Failed to get cloaked attribute for window {Handle} ({ProcessName}), HRESULT: {HResult}",
                hwnd, System.IO.Path.GetFileName(processImageName), hresult);
        }

        return new ApplicationWindow(hwnd, title, (int)processId, processImageName, placement.showCmd, position, size,
            style, styleEx, cloaked != 0);
    }

    private ISet<(HWND Handle, WINDOW_STYLE Style, WindowStyleEx StyleEx)> GetVisibleWindowsHandles()
    {
        var result = new HashSet<(HWND Handle, WINDOW_STYLE Style, WindowStyleEx StyleEx)>();
        var shellWindow = PInvoke.GetShellWindow();

        PInvoke.EnumWindows(Enumerator, nint.Zero);

        return result;

        BOOL Enumerator(HWND hwnd, LPARAM lParam)
        {
            var style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            var styleEx = (WindowStyleEx)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            if (hwnd != shellWindow && PInvoke.IsWindowVisible(hwnd) && PInvoke.IsWindow(hwnd))
            {
                result.Add((hwnd, style, styleEx));
            }

            return true;
        }
    }

    private static unsafe void GetWindowThreadProcessId(HWND hwnd, out uint processId)
    {
        fixed (uint* processIdPtr = &processId)
        {
            PInvoke.GetWindowThreadProcessId(hwnd, processIdPtr);
        }
    }

    private string GetWindowText(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);

        if (length == 0)
        {
            return string.Empty;
        }

        var title = length <= 256 ? stackalloc char[256] : new char[length + 1];
        unsafe
        {
            fixed (char* titlePtr = title)
            {
                length = PInvoke.GetWindowText(hwnd, titlePtr, title.Length); // returned length does not include null terminator
            }
        }

        return title[..length].ToString();
    }
}
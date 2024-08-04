using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.System.Threading;
using System.Runtime.InteropServices;
using System.Buffers;

namespace AppSwitcher.WindowDiscovery;

internal class WindowHelper
{
#pragma warning disable IDE1006 // Naming Styles
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;
#pragma warning restore IDE1006 // Naming Styles

    private readonly ILogger<WindowHelper> _logger;

    public WindowHelper(ILogger<WindowHelper> logger)
    {
        this._logger = logger;
    }

    public IList<ApplicationWindow> GetWindows()
    {
        var sw = Stopwatch.StartNew();
        var windows = GetVisibleWindowsHandles();

        var result = windows
            .Select(w => GetApplicationWindow(w.Handle, w.Style, w.StyleEx))
            .Where(item => item != null && item.IsValidWindow)
            .Select(item => item!)
            .ToList();

        _logger.LogTrace($"Found {result.Count} non-empty windows in {sw.ElapsedMilliseconds}ms");
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
        if (_logger.IsEnabled(level))
        {
            foreach (var item in result)
            {
                _logger.Log(level, "PID/Handle: {ProcessId}/{Handle}, {ProcessName}, {Title}, {State}, {WindowStyle}, {WindowStyleEx}",
                item.ProcessId, item.Handle, item.ProcessImageName, item.Title, item.State, WindowStyleHelpers.GetString(item.Style), WindowStyleHelpers.GetString(item.StyleEx));
            }
        }
    }

    public void LogAllWindows()
    {
        var sw = Stopwatch.StartNew();
        var windows = GetVisibleWindowsHandles();
        var result = windows
            .Select(w => GetApplicationWindow(w.Handle, w.Style, w.StyleEx))
            .Where(item => item != null)
            .Select(item => item!)
            .ToList();

        _logger.LogInformation($"Found {result.Count} windows in {sw.ElapsedMilliseconds}ms");
        LogWindows(LogLevel.Information, result);
    }

    private ApplicationWindow? GetApplicationWindow(HWND hwnd, WINDOW_STYLE style, WindowStyleEx styleEx)
    {
        WINDOWPLACEMENT placement = new();
        PInvoke.GetWindowPlacement(hwnd, ref placement);
        var window = placement.rcNormalPosition;

        var title = GetWindowText(hwnd);
        GetWindowThreadProcessId(hwnd, out var processId);

        var position = new Point(window.left, window.top);
        var size = new Size(window.right - window.left, window.bottom - window.top);

        using var processHandle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (processHandle.IsInvalid)
        {
            _logger.LogWarning($"Failed to open process {processId}");
            return null;
        }

        var processImageName = GetProcessImageName((HANDLE)processHandle.DangerousGetHandle());
        if (processImageName is null)
        {
            _logger.LogWarning($"Failed to get process image name for process {processId}");
            return null;
        }

        return new ApplicationWindow(hwnd, title, (int)processId, processImageName, placement.showCmd, position, size, style, styleEx);
    }

    private ISet<(HWND Handle, WINDOW_STYLE Style, WindowStyleEx StyleEx)> GetVisibleWindowsHandles()
    {
        var result = new HashSet<(HWND Handle, WINDOW_STYLE Style, WindowStyleEx StyleEx)>();
        var shellWindow = PInvoke.GetShellWindow();

        WNDENUMPROC enumerator = delegate (HWND hwnd, LPARAM lParam)
        {
            // exclude popup windows
            var style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            var styleEx = (WindowStyleEx)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            if (hwnd != shellWindow && PInvoke.IsWindowVisible(hwnd) && PInvoke.IsWindow(hwnd))
            {
                result.Add((hwnd, style, styleEx));
            }

            return true;
        };

        PInvoke.EnumWindows(enumerator, nint.Zero);

        return result;
    }

    private unsafe static uint GetWindowThreadProcessId(HWND hwnd, out uint processId)
    {
        fixed (uint* lpdwProcessId = &processId)
        {
            return PInvoke.GetWindowThreadProcessId(hwnd, lpdwProcessId);
        }
    }

    private string GetWindowText(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);

        if (length == 0)
        {
            return string.Empty;
        }

        Span<char> title = length <= 256 ? stackalloc char[256] : new char[length + 1];
        unsafe
        {
            fixed (char* titlePtr = title)
            {
                length = PInvoke.GetWindowText(hwnd, titlePtr, title.Length); // returned length does not include null terminator
            }
        }

        return title[..length].ToString();
    }

    private unsafe static string? GetProcessImageName(HANDLE handle)
    {
        const int startLength = (int)PInvoke.MAX_PATH;

        Span<char> buffer = stackalloc char[startLength + 1];
        char[]? rentedArray = null;

        try
        {
            while (true)
            {
                uint length = (uint)buffer.Length;
                fixed (char* pinnedBuffer = &MemoryMarshal.GetReference(buffer))
                {
                    if (PInvoke.QueryFullProcessImageName(handle, 0, pinnedBuffer, &length))
                    {
                        return buffer[..(int)length].ToString();
                    }
                    else if (Marshal.GetLastPInvokeError() != ERROR_INSUFFICIENT_BUFFER)
                    {
                        return null;
                    }
                }

                char[]? toReturn = rentedArray;
                buffer = rentedArray = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
                if (toReturn is not null)
                {
                    ArrayPool<char>.Shared.Return(toReturn);
                }
            }
        }
        finally
        {
            if (rentedArray is not null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }
    }
}
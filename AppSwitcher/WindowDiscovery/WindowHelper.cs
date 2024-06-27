using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

internal class WindowHelper
{
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
            .Where(w => !w.Style.HasFlag(WindowStyle.WS_POPUP))
            .Select(w => GetApplicationWindow(w.Handle, w.Style))
            .Where(item => item != null && item.Size != Size.Empty && !string.IsNullOrEmpty(item.Title))
            .Select(item => item!)
            .ToList();

        _logger.LogTrace($"Found {result.Count} non-empty windows in {sw.ElapsedMilliseconds}ms");
        LogWindows(result);

        return result;
    }

    public ApplicationWindow? GetCurrentWindow()
    {
        var hwnd = PInvoke.GetForegroundWindow();

        var style = (WindowStyle)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

        while (style.HasFlag(WindowStyle.WS_CHILD) || style.HasFlag(WindowStyle.WS_POPUP))
        {
            hwnd = PInvoke.GetParent(hwnd);
            style = (WindowStyle)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        }

        return GetApplicationWindow(hwnd, style);
    }

    public void LogWindows(IEnumerable<ApplicationWindow> result)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            foreach (var item in result)
            {
                _logger.LogTrace("PID/Handle: {ProcessId}/{Handle}, {ProcessName}, {Title}, {State}", item.ProcessId, item.Handle, item.ProcessImageName, item.Title, item.State);
            }
        }
    }

    public void LogAllWindows()
    {
        var sw = Stopwatch.StartNew();
        var windows = GetVisibleWindowsHandles();
        var result = windows
            .Select(w => GetApplicationWindow(w.Handle, w.Style))
            .Where(item => item != null)
            .Select(item => item!)
            .ToList();

        _logger.LogInformation($"Found {result.Count} windows in {sw.ElapsedMilliseconds}ms");

        foreach (var item in result)
        {
            _logger.LogInformation("PID/Handle: {ProcessId}/{Handle}, {ProcessName}, {Title}, {State}, {WindowStyle}", item.ProcessId, item.Handle, item.ProcessImageName, item.Title, item.State, WindowStyleHelpers.GetString(item.Style));
        }
    }

    private ApplicationWindow? GetApplicationWindow(HWND hwnd, WindowStyle style)
    {
        WINDOWPLACEMENT placement = new();
        PInvoke.GetWindowPlacement(hwnd, ref placement);
        var window = placement.rcNormalPosition;

        var title = GetWindowText(hwnd);
        GetWindowThreadProcessId(hwnd, out var processId);

        var position = new Point(window.left, window.top);
        var size = new Size(window.right - window.left, window.bottom - window.top);

        var processHandle = PInvoke.OpenProcess(Windows.Win32.System.Threading.PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            _logger.LogWarning($"Failed to open process {processId}");
            return null;
        }

        var processImageName = GetProcessImageName(processHandle);
        if (processImageName is null)
        {
            _logger.LogWarning($"Failed to get process image name for process {processId}");
            return null;
        }

        if (PInvoke.CloseHandle(processHandle) == false)
        {
            _logger.LogWarning($"Failed to close process handle for process {processId}");
        }

        return new ApplicationWindow(hwnd, title, (int)processId, processImageName, placement.showCmd, position, size, style);
    }

    private ISet<(HWND Handle, WindowStyle Style)> GetVisibleWindowsHandles()
    {
        var result = new HashSet<(HWND Handle, WindowStyle Style)>();
        var shellWindow = PInvoke.GetShellWindow();

        WNDENUMPROC enumerator = delegate (HWND hwnd, LPARAM lParam)
        {
            // exclude popup windows
            var style = (WindowStyle)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            
            if (hwnd != shellWindow && PInvoke.IsWindowVisible(hwnd) && PInvoke.IsWindow(hwnd))
            {
                result.Add((hwnd, style));
            }

            return true;
        };

        PInvoke.EnumWindows(enumerator, nint.Zero);

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
            length = PInvoke.GetWindowText(hwnd, p, length + 1);
        }

        return s[0..(int)length];
    }

    internal unsafe static string? GetProcessImageName(HANDLE handle)
    {
        string s = new('_', 256);
        uint length = (uint)s.Length;
        fixed (char* p = s)
        {
            if (!PInvoke.QueryFullProcessImageName(handle, 0, p, &length))
            {
                return null;
            }
        }

        return s[0..(int)length];
    }
}

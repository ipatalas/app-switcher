using AppSwitcher.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

internal class WindowEnumerator(ILogger<WindowEnumerator> logger, IProcessPathExtractor processPathExtractor, ProcessInspector processInspector)
{
    public List<ApplicationWindow> GetWindows()
    {
        var sw = Stopwatch.StartNew();
        var handles = GetVisibleWindowsHandles();

        var result = handles
            .Select(GetApplicationWindow)
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

        return GetApplicationWindow(hwnd);
    }

    public record FocusedAppWindows(ApplicationWindow? FocusedWindow, List<ApplicationWindow> AllWindows)
    {
        public static FocusedAppWindows Empty => new(null, []);

        public int Count => AllWindows.Count;

        public int FocusedWindowIndex =>
            FocusedWindow is null ? -1 : AllWindows.FindIndex(w => w.Handle == FocusedWindow.Handle);

        public ApplicationWindow this[int index]
        {
            get { return AllWindows[index]; }
        }
    }
    /// <summary>
    /// This is strictly for the Modifier + Digit functionality
    /// This will get all visible windows for currently focused app if it's configured as NextWindow cycle mode
    /// </summary>
    /// <param name="allWindows">All visible windows to check</param>
    /// <param name="applications">All configured applications</param>
    /// <returns>maximum of 10 windows with stable ordering</returns>
    public FocusedAppWindows GetFocusedAppWindows(
        IReadOnlyList<ApplicationWindow> allWindows,
        IReadOnlyList<ApplicationConfiguration> applications)
    {
        var currentWindow = GetCurrentWindow();
        if (currentWindow is null)
        {
            return FocusedAppWindows.Empty;
        }

        var currentProcessName = Path.GetFileName(currentWindow.ProcessImageName).ToLowerInvariant();
        var focusedApp = applications.FirstOrDefault(a =>
            a.ProcessName.Equals(currentProcessName, StringComparison.InvariantCultureIgnoreCase) &&
            a.CycleMode == CycleMode.NextWindow);

        if (focusedApp is null)
        {
            return FocusedAppWindows.Empty;
        }

        var focusedAppWindows = allWindows
            .Where(w => w.ProcessImageName.EndsWith(focusedApp.ProcessName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static w => (IntPtr)w.Handle) // stable order, independent of Z-order / focus
            .Take(10) // only 10 digit keys available
            .ToList();

        return new FocusedAppWindows(currentWindow, focusedAppWindows);
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
        var handles = GetVisibleWindowsHandles();
        var result = handles
            .Select(GetApplicationWindow)
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

    private unsafe ApplicationWindow? GetApplicationWindow(HWND hwnd)
    {
        WINDOWPLACEMENT placement = new();
        PInvoke.GetWindowPlacement(hwnd, ref placement);
        var window = placement.rcNormalPosition;

        var title = GetWindowText(hwnd);
        GetWindowThreadProcessId(hwnd, out var processId);

        var position = new Point(window.left, window.top);
        var size = new Size(window.right - window.left, window.bottom - window.top);
        if (size.IsEmpty)
        {
            return null;
        }

        var processImageName = processPathExtractor.GetProcessImageName(processId);
        if (processImageName is null)
        {
            return null;
        }

        var style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var styleEx = (WindowStyleEx)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        var cloaked = 0;
        var hresult = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(int));
        if (hresult != 0)
        {
            logger.LogWarning("Failed to get cloaked attribute for window {Handle} ({ProcessName}), HRESULT: {HResult}",
                hwnd, Path.GetFileName(processImageName), hresult);
        }

        var needsElevation = processInspector.NeedsElevation(processId);

        return new ApplicationWindow(hwnd, title, processId, processImageName, placement.showCmd, position, size,
            style, styleEx, cloaked != 0, needsElevation);
    }

    private ISet<HWND> GetVisibleWindowsHandles()
    {
        var result = new HashSet<HWND>();
        var shellWindow = PInvoke.GetShellWindow();

        PInvoke.EnumWindows(Enumerator, nint.Zero);

        return result;

        BOOL Enumerator(HWND hwnd, LPARAM lParam)
        {
            if (hwnd != shellWindow && PInvoke.IsWindowVisible(hwnd) && PInvoke.IsWindow(hwnd))
            {
                result.Add(hwnd);
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
using AppSwitcher.Configuration;
using Microsoft.Extensions.Logging;

namespace AppSwitcher.WindowDiscovery;

internal interface IWindowEnumerator
{
    List<ApplicationWindow> GetWindows();

    /// <summary>
    /// Get cached windows if available - 10 seconds cache
    /// </summary>
    List<ApplicationWindow> GetCachedWindows();

    ApplicationWindow? GetCurrentWindow();

    /// <summary>
    /// This is strictly for the Modifier + Digit functionality
    /// This will get all visible windows for currently focused app if it's configured as NextWindow cycle mode
    /// </summary>
    /// <param name="allWindows">All visible windows to check</param>
    /// <param name="applications">All configured applications</param>
    /// <returns>maximum of 10 windows with stable ordering</returns>
    WindowEnumerator.FocusedAppWindows GetFocusedAppWindows(
        IReadOnlyList<ApplicationWindow> allWindows,
        IReadOnlyList<ApplicationConfiguration> applications);

    void LogWindows(LogLevel level, IEnumerable<ApplicationWindow> result);
    /// <summary>
    /// Gets all windows including windows irrelevant (IsValidWindow = false) for AppSwitcher
    /// </summary>
    List<ApplicationWindow> GetAllWindows();
    void LogAllWindows();

    /// <summary>
    /// Returns the count of all unique app choices using the cached window list.
    /// </summary>
    int GetTotalChoicesCount();
}
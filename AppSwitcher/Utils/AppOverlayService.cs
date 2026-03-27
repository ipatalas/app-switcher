using AppSwitcher.Configuration;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.Windows;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace AppSwitcher.Utils;

internal class AppOverlayService(
    AppOverlayWindow window,
    AppOverlayViewModel viewModel,
    WindowHelper windowHelper,
    IconExtractor iconExtractor,
    TitleSuffixHelper titleSuffixHelper,
    ILogger<AppOverlayService> logger)
{
    private record WindowSnapshot(string Title, string ProcessPath, bool IsActive = false);
    private record AppSnapshot(Key Key, string DisplayName, string ProcessPath, bool IsRunning);

    public bool IsVisible { get; private set; }

    public void Show(IReadOnlyList<ApplicationConfiguration> applications)
    {
        var allWindows = windowHelper.GetWindows();
        var runningProcessNames = allWindows
            .Select(w => Path.GetFileName(w.ProcessImageName).ToLowerInvariant())
            .ToHashSet();

        var (focusedAppName, focusedWindows) = GetFocusedWindowData(allWindows, applications);
        var appSnapshots = applications
            .Select(app => new AppSnapshot(
                app.Key,
                Path.GetFileNameWithoutExtension(app.ProcessName),
                app.ProcessPath,
                runningProcessNames.Contains(app.ProcessName.ToLowerInvariant())))
            .ToList();

        Application.Current.Dispatcher.BeginInvoke(() => ApplyToViewModel(focusedWindows, focusedAppName, appSnapshots));
    }

    public void Hide()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            window.Hide();
            IsVisible = false;
            logger.LogDebug("Overlay hidden");
        });
    }

    private (string? AppName, List<WindowSnapshot> Windows) GetFocusedWindowData(
        IReadOnlyList<ApplicationWindow> allWindows,
        IReadOnlyList<ApplicationConfiguration> applications)
    {
        var focusedWindows = windowHelper.GetFocusedAppWindows(allWindows, applications);
        if (focusedWindows.Count < 1)
        {
            return (null, []);
        }

        var currentWindow = windowHelper.GetCurrentWindow();
        var focusedWindow = focusedWindows.FocusedWindow!;
        var commonSuffix = titleSuffixHelper.FindCommonSuffix(focusedWindows.AllWindows.Select(w => w.Title).ToList());
        var snapshots = focusedWindows.AllWindows
            .Select(w => new WindowSnapshot(
                titleSuffixHelper.StripSuffix(w.Title, commonSuffix),
                focusedWindow.ProcessImageName,
                w.Handle == currentWindow?.Handle))
            .ToList();
        var appName = focusedWindow.GetProductName() ?? Path.GetFileNameWithoutExtension(focusedWindow.ProcessImageName);

        return (appName, snapshots);
    }

    private void ApplyToViewModel(
        List<WindowSnapshot> focusedWindows,
        string? focusedAppName,
        List<AppSnapshot> appSnapshots)
    {
        // Icon extraction and BitmapImage construction must happen on the UI thread
        var focusedWindowItems = focusedWindows
            .Select((w, i) => new OverlayAppItem(IndexToKey(i), w.Title, iconExtractor.GetByProcessPath(w.ProcessPath), w.IsActive))
            .ToList();

        var processPath = focusedWindows.FirstOrDefault()?.ProcessPath;

        var running = new List<OverlayAppItem>();
        var launchable = new List<OverlayAppItem>();
        foreach (var app in appSnapshots)
        {
            var isActive = app.ProcessPath == processPath;
            var item = new OverlayAppItem(app.Key, app.DisplayName, iconExtractor.GetByProcessPath(app.ProcessPath), isActive);
            (app.IsRunning ? running : launchable).Add(item);
        }

        viewModel.Update(focusedWindowItems, focusedAppName, running, launchable);

        // Show the window after WPF's data-binding (priority 8) and render (priority 7) passes
        // to avoid showing a blank/stale frame before content is ready.
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            window.Show();
            IsVisible = true;
            logger.LogDebug(
                "Overlay shown: {Windows} focused windows, {Running} running, {Launchable} launchable apps",
                focusedWindowItems.Count, running.Count, launchable.Count);
        });
    }

    // Key.D0 = 34, Key.D1 = 35, …, Key.D9 = 43 — sequential enum values; index 9 wraps to D0
    private static Key IndexToKey(int i) => i == 9 ? Key.D0 : Key.D1 + i;
}
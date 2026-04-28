using AppSwitcher.Configuration;
using AppSwitcher.Extensions;
using AppSwitcher.Input;
using AppSwitcher.Stats;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.Windows;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;

namespace AppSwitcher.Overlay;

internal class AppOverlayService(
    AppOverlayWindow window,
    AppOverlayViewModel viewModel,
    AppRegistryCache appRegistryCache,
    IWindowEnumerator windowEnumerator,
    IconExtractor iconExtractor,
    WindowTitleParser windowTitleParser,
    DynamicModeService dynamicModeService,
    IPackagedAppsService packagedAppsService,
    ILogger<AppOverlayService> logger)
{
    private record WindowSnapshot(string Title, string ProcessPath, bool IsActive = false);
    private record AppSnapshot(Key Key, string DisplayName, string ProcessPath, string? PackagedAppIconPath, bool IsRunning, bool NeedsElevation);

    public bool IsVisible { get; private set; }

    private volatile int _showToken;

    public void Show(IReadOnlyList<ApplicationConfiguration> applications, bool dynamicModeEnabled)
    {
        var token = Interlocked.Increment(ref _showToken);

        var measureTime = logger.MeasureTime("Show overlay prep");
        var allWindows = windowEnumerator.GetWindows();
        var runningProcesses = allWindows
            .DistinctBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(n => n.ProcessName, StringComparer.OrdinalIgnoreCase);

        var needsElevationProcessNames = allWindows
            .Where(w => w.NeedsElevation)
            .Select(w => w.ProcessName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (focusedAppName, focusedWindows) = GetFocusedWindowData(allWindows, applications);
        var appSnapshots = applications
            .Select(app => new AppSnapshot(
                app.Key,
                appRegistryCache.GetDisplayName(app.ProcessName),
                app.ProcessPath,
                GetIconPath(app),
                runningProcesses.ContainsKey(app.ProcessName),
                needsElevationProcessNames.Contains(app.ProcessName)))
            .ToList();

        var dynamicSnapshots = dynamicModeEnabled
            ? dynamicModeService.GetAllDynamicApps(applications, allWindows)
                .Select(app => new AppSnapshot(
                    app.Key,
                    appRegistryCache.GetDisplayName(app.ProcessName, app.ProcessPath),
                    app.ProcessPath,
                    GetIconPath(app),
                    IsRunning: true,
                    needsElevationProcessNames.Contains(app.ProcessName)))
                .ToList()
            : [];

        measureTime.Dispose();

        Application.Current.Dispatcher.BeginInvoke(() =>
            ApplyToViewModel(focusedWindows, focusedAppName, appSnapshots, dynamicSnapshots, token));
        return;

        string? GetIconPath(ApplicationConfiguration app)
        {
            if (app.Type == ApplicationType.Win32)
            {
                return null;
            }

            try
            {
                if (app.Aumid != null)
                {
                    return packagedAppsService.GetByAumid(app.Aumid)?.IconPath;
                }

                var runningProcess = runningProcesses.GetValueOrDefault(app.ProcessName);
                var processId = runningProcess?.ProcessId;
                return packagedAppsService
                    .GetByInstalledPath(runningProcess?.ProcessImagePath ?? app.ProcessPath, processId)?.IconPath;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to get icon path for {ProcessPath}", app.ProcessPath);
                return null;
            }
        }
    }

    public void Hide()
    {
        Interlocked.Increment(ref _showToken);

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
        var focusedWindows = windowEnumerator.GetFocusedAppWindows(allWindows, applications);
        if (focusedWindows.Count < 1)
        {
            return (null, []);
        }

        var focusedWindow = focusedWindows.FocusedWindow!;
        var commonSuffix = windowTitleParser.FindCommonSuffix(focusedWindows.AllWindows.Select(w => w.Title).ToList());
        var snapshots = focusedWindows.AllWindows
            .Select(w => new WindowSnapshot(
                windowTitleParser.StripSuffix(w.Title, commonSuffix),
                focusedWindow.ProcessImagePath,
                w.Handle == focusedWindow.Handle))
            .ToList();
        var appName = focusedWindow.GetProductName() ?? Path.GetFileNameWithoutExtension(focusedWindow.ProcessImagePath);

        return (appName, snapshots);
    }

    private void ApplyToViewModel(
        List<WindowSnapshot> focusedWindows,
        string? focusedAppName,
        List<AppSnapshot> appSnapshots,
        List<AppSnapshot> dynamicSnapshots,
        int token)
    {
        // Icon extraction and BitmapImage construction must happen on the UI thread
        var focusedWindowItems = focusedWindows
            .Select((w, i) => new OverlayAppItem(IndexToKey(i), w.Title, iconExtractor.GetByProcessPath(w.ProcessPath), w.IsActive))
            .ToList();

        var processPath = focusedWindows.FirstOrDefault()?.ProcessPath ?? windowEnumerator.GetCurrentWindow()?.ProcessImagePath;

        var running = new List<OverlayAppItem>();
        var launchable = new List<OverlayAppItem>();
        foreach (var app in appSnapshots)
        {
            var item = CreateOverlayAppItem(app);
            (app.IsRunning ? running : launchable).Add(item);
        }

        var dynamic = dynamicSnapshots
            .Select(CreateOverlayAppItem)
            .ToList();

        viewModel.Update(focusedWindowItems, focusedAppName, running, launchable, dynamic);

        // Show the window after WPF's data-binding (priority 8) and render (priority 7) passes
        // to avoid showing a blank/stale frame before content is ready.
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (_showToken != token)
            {
                return;
            }

            window.Show();
            IsVisible = true;
            logger.LogDebug(
                "Overlay shown: {Windows} focused windows, {Running} running, {Launchable} launchable, {Dynamic} dynamic apps",
                focusedWindowItems.Count, running.Count, launchable.Count, dynamic.Count);
        });

        OverlayAppItem CreateOverlayAppItem(AppSnapshot app)
        {
            var isActive = app.ProcessPath == processPath;
            var icon = app.PackagedAppIconPath != null ? iconExtractor.GetByIconPath(app.PackagedAppIconPath) : iconExtractor.GetByProcessPath(app.ProcessPath);
            var item = new OverlayAppItem(app.Key, app.DisplayName, icon, isActive, app.NeedsElevation);
            return item;
        }
    }

    // Key.D0 = 34, Key.D1 = 35, …, Key.D9 = 43 — sequential enum values; index 9 wraps to D0
    private static Key IndexToKey(int i) => i == 9 ? Key.D0 : Key.D1 + i;
}
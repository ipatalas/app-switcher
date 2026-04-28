using AppSwitcher.Configuration;
using AppSwitcher.Extensions;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows.Input;

namespace AppSwitcher.Input;

internal class DynamicModeService(
    IWindowEnumerator windowEnumerator,
    IAppNameResolver appNameResolver,
    ILogger<DynamicModeService> logger,
    IPackagedAppsService packagedAppsService)
{
    public IReadOnlyList<ApplicationConfiguration> GetAllDynamicApps(
        IReadOnlyList<ApplicationConfiguration> staticApps,
        IReadOnlyList<ApplicationWindow> windows)
    {
        using var _ = logger.MeasureTime("GetAllDynamicApps");
        var staticKeys = staticApps.Select(a => a.Key).ToHashSet();
        var excludedProcessNames = GetExcludedProcessNames(staticApps);
        var packagedApps = packagedAppsService.GetInstalledPaths();

        return windows
            .DistinctBy(w => w.ProcessImagePath, StringComparer.OrdinalIgnoreCase)
            .Where(x => !excludedProcessNames.Contains(x.ProcessName))
            .Select(w => (Window: w, Key: appNameResolver.GetDynamicKey(w.ProcessName, w.ProcessImagePath)))
            .Where(x => x.Key.HasValue && !staticKeys.Contains(x.Key.Value))
            .Select(x => new ApplicationConfiguration(
                Key: x.Key!.Value,
                ProcessPath: x.Window.ProcessImagePath,
                CycleMode: CycleMode.NextApp,
                StartIfNotRunning: false,
                Type: packagedApps.Contains(Path.GetDirectoryName(x.Window.ProcessImagePath)!) ? ApplicationType.Packaged : ApplicationType.Win32))
            .ToList();
    }

    public IReadOnlyList<ApplicationConfiguration> GetAppsForKey(
        Key letter,
        IReadOnlyList<ApplicationConfiguration> staticApps)
    {
        if (staticApps.Any(a => a.Key == letter))
        {
            return [];
        }

        var windows = windowEnumerator.GetCachedWindows();
        var excludedProcessNames = GetExcludedProcessNames(staticApps);

        return windows
            .Where(w => !excludedProcessNames.Contains(w.ProcessName))
            .Where(w => appNameResolver.GetDynamicKey(w.ProcessName, w.ProcessImagePath) == letter)
            .DistinctBy(w => w.ProcessImagePath, StringComparer.OrdinalIgnoreCase)
            .Select(w => new ApplicationConfiguration(
                Key: letter,
                ProcessPath: w.ProcessImagePath,
                CycleMode: CycleMode.NextApp,
                StartIfNotRunning: false))
            .ToList();
    }

    private static HashSet<string> GetExcludedProcessNames(IReadOnlyList<ApplicationConfiguration> staticApps)
    {
        return staticApps
            .Select(a => a.ProcessName)
            .Concat(["appswitcher.exe"])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
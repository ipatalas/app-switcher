using AppSwitcher.Stats;
using System.IO;
using System.Windows.Media;

namespace AppSwitcher.WindowDiscovery;

internal record RunningApplicationInfo(uint ProcessId, string ProcessName, string ProcessImagePath, string DisplayName, ImageSource? Icon, bool IsPackagedApp);

internal class RunningApplicationsService(
    IWindowEnumerator windowEnumerator,
    IconExtractor iconExtractor,
    IPackagedAppsService packagedAppsService,
    AppRegistryCache appRegistryCache)
{
    public IReadOnlyList<RunningApplicationInfo> GetRunningApplications(IEnumerable<string> excludedProcessNames)
    {
        var excluded = excludedProcessNames
            .Select(n => Path.GetFileName(n).ToLowerInvariant())
            .Concat(["appswitcher.exe"])
            .ToHashSet();

        var installedPackages = packagedAppsService.GetInstalledPackages();

        return windowEnumerator.GetWindows()
            .Select(w => (Name: Path.GetFileName(w.ProcessImagePath), Path: w.ProcessImagePath, Id: w.ProcessId))
            .DistinctBy(item => item.Name.ToLowerInvariant())
            .Where(item => !excluded.Contains(item.Name.ToLowerInvariant()))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item =>
            {
                var isPackagedApp = installedPackages.Contains(item.Path);
                var icon = isPackagedApp
                    ? iconExtractor.GetByIconPath(packagedAppsService.GetByInstalledPath(item.Path, item.Id)?.IconPath)
                    : iconExtractor.GetByProcessPath(item.Path);

                return new RunningApplicationInfo(
                    ProcessId: item.Id,
                    ProcessName: item.Name,
                    ProcessImagePath: item.Path,
                    DisplayName: appRegistryCache.GetDisplayName(item.Name),
                    Icon: icon,
                    IsPackagedApp: isPackagedApp);
            })
            .ToList();
    }
}
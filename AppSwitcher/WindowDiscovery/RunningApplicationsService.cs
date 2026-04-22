using System.IO;
using System.Windows.Media;

namespace AppSwitcher.WindowDiscovery;

internal record RunningApplicationInfo(uint ProcessId, string ProcessName, string ProcessImagePath, ImageSource? Icon, bool IsPackagedApp);

internal class RunningApplicationsService(IWindowEnumerator windowEnumerator, IconExtractor iconExtractor, IPackagedAppsService packagedAppsService)
{
    public IReadOnlyList<RunningApplicationInfo> GetRunningApplications(IEnumerable<string> excludedProcessNames)
    {
        var excluded = excludedProcessNames
            .Select(n => Path.GetFileName(n).ToLowerInvariant())
            .Concat(["appswitcher.exe"])
            .ToHashSet();

        var packagedApps = packagedAppsService.GetInstalledPaths();

        return windowEnumerator.GetWindows()
            .Select(w => (Name: Path.GetFileName(w.ProcessImagePath), Path: w.ProcessImagePath, Id: w.ProcessId, Directory: Path.GetDirectoryName(w.ProcessImagePath)!))
            .DistinctBy(item => item.Name.ToLowerInvariant())
            .Where(item => !excluded.Contains(item.Name.ToLowerInvariant()))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item =>
            {
                var isPackagedApp = packagedApps.Contains(item.Directory);
                var icon = isPackagedApp
                    ? iconExtractor.GetByIconPath(packagedAppsService.GetByInstalledPath(item.Path, item.Id)?.IconPath)
                    : iconExtractor.GetByProcessPath(item.Path);

                return new RunningApplicationInfo(
                    ProcessId: item.Id,
                    ProcessName: item.Name,
                    ProcessImagePath: item.Path,
                    Icon: icon,
                    IsPackagedApp: isPackagedApp);
            })
            .ToList();
    }
}
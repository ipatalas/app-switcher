using System.IO;
using System.Windows.Media;

namespace AppSwitcher.WindowDiscovery;

internal record RunningApplicationInfo(string ProcessName, string ProcessImagePath, ImageSource? Icon, bool IsPackagedApp);

internal class RunningApplicationsService(WindowEnumerator windowEnumerator, IconExtractor iconExtractor, IPackagedAppsService packagedAppsService)
{
    public IReadOnlyList<RunningApplicationInfo> GetRunningApplications(IEnumerable<string> excludedProcessNames)
    {
        var excluded = excludedProcessNames
            .Select(n => Path.GetFileName(n).ToLowerInvariant())
            .Concat(["appswitcher.exe"])
            .ToHashSet();

        var packagedApps = packagedAppsService.GetInstalledPaths();

        return windowEnumerator.GetWindows()
            .Select(w => (Name: Path.GetFileName(w.ProcessImagePath), Path: w.ProcessImagePath, Directory: Path.GetDirectoryName(w.ProcessImagePath)!))
            .DistinctBy(item => item.Name.ToLowerInvariant())
            .Where(item => !excluded.Contains(item.Name.ToLowerInvariant()))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new RunningApplicationInfo(
                ProcessName: item.Name,
                ProcessImagePath: item.Path,
                Icon: iconExtractor.GetByProcessPath(item.Path),
                IsPackagedApp: packagedApps.Contains(item.Directory)))
            .ToList();
    }
}
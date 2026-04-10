using System.IO;
using System.Windows.Media;

namespace AppSwitcher.WindowDiscovery;

internal record RunningApplicationInfo(string ProcessName, string ProcessImageName, ImageSource? Icon, bool IsPackagedApp);

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
            .Select(w => (Name: Path.GetFileName(w.ProcessImageName), Path: w.ProcessImageName, Directory: Path.GetDirectoryName(w.ProcessImageName)!))
            .DistinctBy(item => item.Name.ToLowerInvariant())
            .Where(item => !excluded.Contains(item.Name.ToLowerInvariant()))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new RunningApplicationInfo(
                ProcessName: item.Name,
                ProcessImageName: item.Path,
                Icon: iconExtractor.GetByProcessPath(item.Path),
                IsPackagedApp: packagedApps.Contains(item.Directory)))
            .ToList();
    }
}
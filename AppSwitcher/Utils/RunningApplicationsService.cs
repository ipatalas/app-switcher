using AppSwitcher.WindowDiscovery;
using System.IO;
using System.Windows.Media;

namespace AppSwitcher.Utils;

internal record RunningApplicationInfo(string ProcessName, string ProcessImageName, ImageSource? Icon);

internal class RunningApplicationsService(WindowHelper windowHelper, IconExtractor iconExtractor)
{
    public IReadOnlyList<RunningApplicationInfo> GetRunningApplications(IEnumerable<string> excludedProcessNames)
    {
        var excluded = excludedProcessNames
            .Select(n => Path.GetFileName(n).ToLowerInvariant())
            .Concat(["appswitcher.exe"])
            .ToHashSet();

        return windowHelper.GetWindows()
            .Select(w => (Name: Path.GetFileName(w.ProcessImageName), ImagePath: w.ProcessImageName))
            .DistinctBy(item => item.Name.ToLowerInvariant())
            .Where(item => !excluded.Contains(item.Name.ToLowerInvariant()))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new RunningApplicationInfo(
                ProcessName: item.Name,
                ProcessImageName: item.ImagePath,
                Icon: iconExtractor.GetByProcessName(item.ImagePath)))
            .ToList();
    }
}

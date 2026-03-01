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
            .ToHashSet();

        return windowHelper.GetWindows()
            .Select(w => Path.GetFileName(w.ProcessImageName))
            .DistinctBy(name => name.ToLowerInvariant())
            .Where(name => !excluded.Contains(name.ToLowerInvariant()))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new RunningApplicationInfo(
                ProcessName: name,
                ProcessImageName: name,
                Icon: iconExtractor.GetByProcessName(name)))
            .ToList();
    }
}

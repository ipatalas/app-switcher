using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;
using Windows.Management.Deployment;

namespace AppSwitcher.Utils;

public class PackagedAppsService(ILogger<PackagedAppsService> logger)
{
    public IReadOnlySet<string> GetInstalledPaths()
    {
        var sw = Stopwatch.StartNew();
        var packageManager = new PackageManager();
        var result = packageManager.FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main)
            .Select(p => p.InstalledPath).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        logger.LogDebug($"Found {result.Count} installed packages in {sw.ElapsedMilliseconds}ms");
        return result;
    }

    public PackagedAppInfo? GetByInstalledPath(string path)
    {
        var packageManager = new PackageManager();
        var packages = packageManager.FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main);

        var package = packages.FirstOrDefault(p => path.StartsWith(p.InstalledPath, StringComparison.OrdinalIgnoreCase));
        if (package == null)
        {
            return null;
        }

        var appListEntry = package.GetAppListEntries().FirstOrDefault();
        if (appListEntry == null)
        {
            return null;
        }

        return new PackagedAppInfo(Aumid: appListEntry.AppUserModelId,
            IconPath: package.Logo.IsFile ? package.Logo.LocalPath : string.Empty);
    }
}

public sealed record PackagedAppInfo(string Aumid, string? IconPath);
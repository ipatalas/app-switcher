using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace AppSwitcher.WindowDiscovery;

internal class PackagedAppsService(ILogger<PackagedAppsService> logger) : IPackagedAppsService
{
    // this never changes so no expiration
    private readonly Dictionary<string, PackagedAppInfo> _packageCache = new();

    public IReadOnlySet<string> GetInstalledPaths()
    {
        var sw = Stopwatch.StartNew();
        var result = new PackageManager().FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main)
            .Select(p => p.InstalledPath).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        logger.LogDebug($"Found {result.Count} installed packages in {sw.ElapsedMilliseconds}ms");
        return result;
    }

    public string? GetDisplayName(string installPath)
    {
        var packages = new PackageManager()
            .FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main);

        // reading DisplayName for all apps is heavy (~500ms) so better to find relevant package first
        var package = packages.FirstOrDefault(p =>
            installPath.StartsWith(p.InstalledPath, StringComparison.OrdinalIgnoreCase));

        return package == null ? null : package.DisplayName;
    }

    public PackagedAppInfo? GetByInstalledPath(string path, uint? processId)
    {
        var package =
            new PackageManager().FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main)
                .FirstOrDefault(p => path.StartsWith(p.InstalledPath, StringComparison.OrdinalIgnoreCase));

        return package == null ? null : GetPackagedAppInfo(package, processId);
    }

    public PackagedAppInfo? GetByAumid(string? aumid)
    {
        if (string.IsNullOrEmpty(aumid))
        {
            return null;
        }

        var packageManager = new PackageManager();
        var packageFamilyName = aumid.Split('!')[0];

        var package = packageManager.FindPackagesForUser(string.Empty, packageFamilyName).FirstOrDefault();

        return package == null ? null : GetPackagedAppInfo(package);
    }

    private PackagedAppInfo? GetPackagedAppInfo(Package package, uint? processId = null)
    {
        if (_packageCache.TryGetValue(package.InstalledPath, out var cachedApp))
        {
            return cachedApp;
        }

        var aumid = package.GetAppListEntries().FirstOrDefault()?.AppUserModelId;
        if (aumid == null && processId != null)
        {
            aumid = GetAumidByProcessId(processId.Value);
            if (aumid == null)
            {
                return null;
            }
        }

        var result = new PackagedAppInfo(Aumid: aumid!,
            IconPath: package.Logo.IsFile ? package.Logo.LocalPath : string.Empty);

        _packageCache[package.InstalledPath] = result;
        return result;
    }

    private string? GetAumidByProcessId(uint processId)
    {
        using var process =
            PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (process.IsInvalid)
        {
            logger.LogWarning("Failed to open process {ProcessId} to query AUMID: {ErrorCode}", processId,
                Marshal.GetLastWin32Error());
            return null;
        }

        uint length = 0;
        var result = PInvoke.GetApplicationUserModelId(process, ref length);

        if (result == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
        {
            Span<char> buffer = stackalloc char[(int)length];
            result = PInvoke.GetApplicationUserModelId(process, ref length, buffer);
            if (result == WIN32_ERROR.ERROR_SUCCESS)
            {
                return new string(buffer[..((int)length - 1)]); // remove null terminator
            }
        }

        logger.LogWarning("Failed to get AUMID for process {ProcessId}: {ErrorCode}", processId, result);
        return null;
    }
}

public sealed record PackagedAppInfo(string Aumid, string? IconPath);
namespace AppSwitcher.WindowDiscovery;

internal interface IPackagedAppsService
{
    InstalledPackages GetInstalledPackages();
    PackagedAppInfo? GetByInstalledPath(string path, uint? processId);
    PackagedAppInfo? GetByAumid(string? aumid);
    string? GetInstalledPathByAumid(string? aumid);
    string? GetDisplayName(string installPath);
}
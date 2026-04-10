namespace AppSwitcher.WindowDiscovery;

internal interface IPackagedAppsService
{
    IReadOnlySet<string> GetInstalledPaths();
    PackagedAppInfo? GetByInstalledPath(string path);
    PackagedAppInfo? GetByAumid(string? aumid);
}
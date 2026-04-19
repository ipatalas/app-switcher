namespace AppSwitcher.WindowDiscovery;

internal interface IPackagedAppsService
{
    IReadOnlySet<string> GetInstalledPaths();
    PackagedAppInfo? GetByInstalledPath(string path, uint? processId);
    PackagedAppInfo? GetByAumid(string? aumid);
}
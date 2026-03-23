namespace AppSwitcher.Utils;

internal interface IPackagedAppsService
{
    IReadOnlySet<string> GetInstalledPaths();
    PackagedAppInfo? GetByInstalledPath(string path);
}
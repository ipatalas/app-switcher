using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AppSwitcher.Configuration;

internal class PackagedAppPathSanitizer(IPackagedAppsService packagedAppsService, ILogger<PackagedAppPathSanitizer> logger)
{
    /// <summary>
    /// Checks all packaged app paths and fixes any that are stale (e.g. after a package update).
    /// Returns an updated <see cref="Configuration"/> if any paths were corrected, or <c>null</c> if nothing changed.
    /// </summary>
    public Configuration? Sanitize(Configuration config)
    {
        var updatedApps = new List<ApplicationConfiguration>(config.Applications.Count);
        var changed = false;

        foreach (var app in config.Applications)
        {
            if (app.Type != ApplicationType.Packaged || File.Exists(app.ProcessPath))
            {
                updatedApps.Add(app);
                continue;
            }

            var newInstalledPath = packagedAppsService.GetInstalledPathByAumid(app.Aumid);
            if (newInstalledPath is null)
            {
                logger.LogWarning("Packaged app '{ProcessName}' has a stale path and could not be resolved via AUMID '{Aumid}' - it may be uninstalled",
                    app.ProcessName, app.Aumid);
                updatedApps.Add(app);
                continue;
            }

            var exeName = Path.GetFileName(app.ProcessPath);
            var newProcessPath = Path.Combine(newInstalledPath, exeName);
            logger.LogInformation("Updated stale path for '{ProcessName}': '{OldPath}' -> '{NewPath}'",
                app.ProcessName, app.ProcessPath, newProcessPath);

            updatedApps.Add(app with { ProcessPath = newProcessPath });
            changed = true;
        }

        if (!changed)
        {
            return null;
        }

        return config with { Applications = updatedApps };
    }
}

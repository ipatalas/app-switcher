using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;

namespace AppSwitcher.Configuration;

internal class PackagedAppPathSanitizer(IPackagedAppsService packagedAppsService, ILogger<PackagedAppPathSanitizer> logger)
{
    // <Name>_<Version>_<Architecture>_<ResourceId>_<PublisherId> - package names cannot contain underscores
    private static readonly Regex PackageDirectoryRegex = new(
        @"^(?<name>[A-Za-z0-9.-]{3,50})_\d+(\.\d+){3}_[A-Za-z0-9]*_[^_]*_(?<publisherId>[a-z0-9]{13})$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Reclassifies Win32 apps that are actually located in a package directory as packaged apps,
    /// and fixes packaged app paths that are stale (e.g. after a package update).
    /// Returns an updated <see cref="Configuration"/> if anything was corrected, or <c>null</c> if nothing changed.
    /// </summary>
    public Configuration? Sanitize(Configuration config)
    {
        var updatedApps = new List<ApplicationConfiguration>(config.Applications.Count);
        var changed = false;

        foreach (var original in config.Applications)
        {
            var app = original.Type == ApplicationType.Win32
                ? TryConvertToPackaged(original) ?? original
                : original;
            app = TryFixStalePath(app) ?? app;

            changed |= app != original;
            updatedApps.Add(app);
        }

        if (!changed)
        {
            return null;
        }

        return config with { Applications = updatedApps };
    }

    private ApplicationConfiguration? TryConvertToPackaged(ApplicationConfiguration app)
    {
        var packageFamilyName = GetPackageFamilyName(app.ProcessPath);
        if (packageFamilyName is null)
        {
            return null;
        }

        var packagedApp = packagedAppsService.GetByPackageFamilyName(packageFamilyName);
        if (packagedApp is null)
        {
            logger.LogWarning("'{ProcessName}' is located in a package directory but package '{PackageFamilyName}' could not be resolved - it may be uninstalled",
                app.ProcessName, packageFamilyName);
            return null;
        }

        logger.LogInformation("Reclassified '{ProcessName}' as packaged app with AUMID '{Aumid}'",
            app.ProcessName, packagedApp.Aumid);
        return app with { Type = ApplicationType.Packaged, Aumid = packagedApp.Aumid };
    }

    private ApplicationConfiguration? TryFixStalePath(ApplicationConfiguration app)
    {
        if (app.Type != ApplicationType.Packaged || File.Exists(app.ProcessPath))
        {
            return null;
        }

        var newInstalledPath = packagedAppsService.GetInstalledPathByAumid(app.Aumid);
        if (newInstalledPath is null)
        {
            logger.LogWarning("Packaged app '{ProcessName}' has a stale path and could not be resolved via AUMID '{Aumid}' - it may be uninstalled",
                app.ProcessName, app.Aumid);
            return null;
        }

        var exeRelativePath = GetPathRelativeToPackage(app.ProcessPath, newInstalledPath)
                              ?? Path.GetFileName(app.ProcessPath);
        var newProcessPath = Path.Combine(newInstalledPath, exeRelativePath);
        logger.LogInformation("Updated stale path for '{ProcessName}': '{OldPath}' -> '{NewPath}'",
            app.ProcessName, app.ProcessPath, newProcessPath);

        return app with { ProcessPath = newProcessPath };
    }

    /// <summary>
    /// Derives the package family name (<c>&lt;Name&gt;_&lt;PublisherId&gt;</c>) from the package directory
    /// <paramref name="processPath"/> is located in. The family name does not change between package versions,
    /// so it also works for stale paths pointing to an already removed version.
    /// </summary>
    private static string? GetPackageFamilyName(string processPath)
    {
        for (var directory = Path.GetDirectoryName(processPath);
             !string.IsNullOrEmpty(directory);
             directory = Path.GetDirectoryName(directory))
        {
            var match = PackageDirectoryRegex.Match(Path.GetFileName(directory));
            if (match.Success)
            {
                return $"{match.Groups["name"].Value}_{match.Groups["publisherId"].Value}";
            }
        }

        return null;
    }

    /// <summary>
    /// Package updates are installed side by side (<c>&lt;root&gt;\&lt;PackageFullName&gt;</c>), so the stale path is
    /// expected to live in a sibling of <paramref name="newInstalledPath"/>. Returns the part of
    /// <paramref name="stalePath"/> below its package directory (e.g. <c>app\Claude.exe</c>), or <c>null</c>
    /// when <paramref name="stalePath"/> is not located under the same root.
    /// </summary>
    private static string? GetPathRelativeToPackage(string stalePath, string newInstalledPath)
    {
        var packageRoot = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(newInstalledPath));
        if (packageRoot is null)
        {
            return null;
        }

        var relativeToRoot = Path.GetRelativePath(packageRoot, stalePath);
        if (Path.IsPathRooted(relativeToRoot) || relativeToRoot.StartsWith(".." + Path.DirectorySeparatorChar))
        {
            return null;
        }

        // strip the stale package directory, keep everything below it
        var separatorIndex = relativeToRoot.IndexOf(Path.DirectorySeparatorChar);
        return separatorIndex < 0 ? null : relativeToRoot[(separatorIndex + 1)..];
    }
}
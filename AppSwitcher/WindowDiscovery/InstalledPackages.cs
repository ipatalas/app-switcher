using System.IO;

namespace AppSwitcher.WindowDiscovery;

/// <summary>
/// Snapshot of installed package directories used to tell whether a process belongs to a packaged app.
/// </summary>
internal sealed class InstalledPackages
{
    public static readonly InstalledPackages Empty = new([]);

    private readonly HashSet<string> _installedPaths;

    // Packages are installed as <root>\<PackageFullName> and there are only a handful of distinct roots
    // (e.g. WindowsApps, SystemApps), so matching against roots first avoids walking up every process path.
    private readonly string[] _packageRoots;

    public InstalledPackages(IEnumerable<string> installedPaths)
    {
        _installedPaths = installedPaths
            .Select(Path.TrimEndingDirectorySeparator)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _packageRoots = _installedPaths
            .Select(Path.GetDirectoryName)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public int Count => _installedPaths.Count;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="processPath"/> is located anywhere inside an installed package,
    /// including its subdirectories (e.g. <c>&lt;InstalledPath&gt;\app\Claude.exe</c>).
    /// </summary>
    public bool Contains(string processPath)
    {
        foreach (var root in _packageRoots)
        {
            var packageDirectory = GetChildDirectory(root, processPath);
            if (packageDirectory is not null && _installedPaths.Contains(packageDirectory))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the first directory below <paramref name="root"/> on the way to <paramref name="path"/>,
    /// or <c>null</c> when <paramref name="path"/> is not located in a subdirectory of <paramref name="root"/>.
    /// </summary>
    private static string? GetChildDirectory(string root, string path)
    {
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // root may already end with a separator (e.g. "C:\")
        var childStart = root.Length;
        if (!Path.EndsInDirectorySeparator(root))
        {
            if (path.Length <= childStart || !IsDirectorySeparator(path[childStart]))
            {
                return null;
            }

            childStart++;
        }

        var childEnd = path.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], childStart);
        return childEnd < 0 ? null : path[..childEnd];
    }

    private static bool IsDirectorySeparator(char c) =>
        c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
}
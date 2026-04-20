using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace AppSwitcher.WindowDiscovery;

internal class AppNameResolver
{
    /// <summary>
    /// Resolves the display name for a process and returns the corresponding letter key,
    /// or null if the resolved name does not start with a letter.
    /// </summary>
    public Key? GetDynamicKey(string processPath)
    {
        var companyName = GetCompanyName(processPath);
        var processFilename = Path.GetFileNameWithoutExtension(processPath);
        var displayName = ResolveDisplayName(companyName, processFilename);

        if (string.IsNullOrEmpty(displayName))
        {
            return null;
        }

        var firstChar = char.ToUpperInvariant(displayName[0]);
        if (firstChar is < 'A' or > 'Z')
        {
            return null;
        }

        return Key.A + (firstChar - 'A');
    }

    private static string? GetCompanyName(string processPath)
    {
        if (!File.Exists(processPath))
        {
            return null;
        }

        var fileVersionInfo = FileVersionInfo.GetVersionInfo(processPath);
        var companyName = fileVersionInfo.CompanyName;
        return companyName;
    }

    /// <summary>
    /// Pure resolution logic: derives a display name from version info and process filename.
    /// If CompanyName contains "Microsoft" and the filename starts with "ms", the "ms" prefix is stripped.
    /// </summary>
    internal static string ResolveDisplayName(string? companyName, string? processFilename)
    {
        if (string.IsNullOrEmpty(processFilename))
        {
            return string.Empty;
        }

        var name = processFilename;

        var isMicrosoftApp = companyName is not null &&
                             companyName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);

        if (isMicrosoftApp && name.StartsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            name = name[2..];
        }

        return name;
    }

}
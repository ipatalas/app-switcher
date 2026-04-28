using AppSwitcher.Stats;
using System.Windows.Input;

namespace AppSwitcher.WindowDiscovery;

internal class AppNameResolver(IAppRegistryCache appRegistryCache) : IAppNameResolver
{
    /// <summary>
    /// Resolves the display name for a process via <see cref="IAppRegistryCache"/> and returns
    /// the corresponding letter key, or null if the resolved name does not start with a letter.
    /// </summary>
    public Key? GetDynamicKey(string processName, string processPath)
    {
        var displayName = appRegistryCache.GetDisplayName(processName, processPath);

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
}
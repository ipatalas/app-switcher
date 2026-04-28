using System.Windows.Input;

namespace AppSwitcher.WindowDiscovery;

internal interface IAppNameResolver
{
    Key? GetDynamicKey(string processName, string processPath);
}
namespace AppSwitcher.WindowDiscovery;

internal interface IProcessInspector
{
    bool NeedsElevation(uint processId);
    Task<bool> WaitForPotentialElevation(string processPath, int timeoutMs = 2000);
    bool IsWindowsExecutable(string path);
    string GetProcessDisplayName(string path);
}
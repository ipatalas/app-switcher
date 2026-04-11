namespace AppSwitcher.WindowDiscovery;

public interface IProcessPathExtractor
{
    string? GetProcessImagePath(uint processId);
}
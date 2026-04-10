namespace AppSwitcher.WindowDiscovery;

public interface IProcessPathExtractor
{
    string? GetProcessImageName(uint processId);
}
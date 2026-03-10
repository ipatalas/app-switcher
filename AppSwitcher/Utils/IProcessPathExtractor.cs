namespace AppSwitcher.Utils;

public interface IProcessPathExtractor
{
    string? GetProcessImageName(uint processId);
    string? GetPathFromRegistry(string processNameWithExtension);
}

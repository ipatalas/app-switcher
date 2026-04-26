using System.Windows.Media;

namespace AppSwitcher.UI.ViewModels;

internal record AppStatEntry(
    string ProcessName,
    string DisplayName,
    ImageSource? Icon,
    int Switches,
    int Peeks,
    int TotalPeekTimeMs)
{
    public double AvgGlanceSec => Peeks > 0 ? TotalPeekTimeMs / (Peeks * 1000.0) : 0;
}

internal record StaleShortcutEntry(
    string Letter,
    string ProcessName,
    string DisplayName,
    ImageSource? Icon);
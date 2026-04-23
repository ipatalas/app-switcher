using AppSwitcher.Configuration;
using System.Windows.Input;
using System.Windows.Media;

namespace AppSwitcher.UI.ViewModels.Common;

internal class DynamicApplicationViewModel
{
    public Key Key { get; init; }
    public string KeyLetter => Key.ToString();
    public string ProcessName { get; init; } = null!;
    public string ProcessPath { get; init; } = null!;
    public ApplicationType Type { get; init; } = ApplicationType.Win32;
    public ImageSource? ProcessIcon { get; init; }
}
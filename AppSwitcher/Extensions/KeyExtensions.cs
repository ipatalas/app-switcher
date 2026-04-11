using System.Windows.Input;

namespace AppSwitcher.Extensions;

public static class KeyExtensions
{
    public static bool IsLetter(this Key key) => key is >= Key.A and <= Key.Z;
    public static bool IsDigit(this Key key) => key is >= Key.D0 and <= Key.D9;
}
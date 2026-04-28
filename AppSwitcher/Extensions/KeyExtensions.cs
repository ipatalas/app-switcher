using System.Windows.Input;

namespace AppSwitcher.Extensions;

public static class KeyExtensions
{
    public static bool IsLetter(this Key key) => key is >= Key.A and <= Key.Z;
    public static bool IsDigit(this Key key) => key is >= Key.D0 and <= Key.D9;

    public static string ToFriendlyString(this Key key)
    {
        if (key.IsDigit())
        {
            return ((char)('0'+ key - Key.D0)).ToString();
        }

        return key.ToString();
    }
}
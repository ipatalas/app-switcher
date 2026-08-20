using System.Windows.Input;

namespace AppSwitcher.Extensions;

public static class KeyExtensions
{
    public static bool IsLetter(this Key key) => key is >= Key.A and <= Key.Z;
    public static bool IsDigit(this Key key) => key is >= Key.D0 and <= Key.D9;

    public static bool IsPunctuation(this Key key) => key is
        Key.OemTilde or
        Key.OemMinus or
        Key.OemPlus or
        Key.OemOpenBrackets or
        Key.OemCloseBrackets or
        Key.OemPipe or
        Key.OemSemicolon or
        Key.OemQuotes or
        Key.OemComma or
        Key.OemPeriod or
        Key.OemQuestion;

    public static bool IsApplicationKey(this Key key) => key.IsLetter() || key.IsPunctuation();

    public static string ToFriendlyString(this Key key) => key switch
    {
        _ when key.IsDigit() => ((char)('0' + key - Key.D0)).ToString(),
        Key.OemTilde => "`",
        Key.OemMinus => "-",
        Key.OemPlus => "=",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemPipe => "\\",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemQuestion => "/",
        _ => key.ToString()
    };
}
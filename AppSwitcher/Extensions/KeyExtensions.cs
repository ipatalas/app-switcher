using System.Windows.Input;

namespace AppSwitcher.Extensions;

public static class KeyExtensions
{
    extension(Key key)
    {
        public bool IsLetter => key is >= Key.A and <= Key.Z;
        public bool IsDigit => key is >= Key.D0 and <= Key.D9;

        public string ToFriendlyString()
        {
            if (key.IsDigit)
            {
                return ((char)('0' + key - Key.D0)).ToString();
            }

            return key.ToString();
        }
    }
}
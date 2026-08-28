using System.Windows.Input;
using AppSwitcher.Extensions;
using AwesomeAssertions;
using Xunit;

namespace AppSwitcher.Tests.Extensions;

public class KeyExtensionsTests
{
    [Theory]
    [InlineData(Key.A, "A")]
    [InlineData(Key.Z, "Z")]
    [InlineData(Key.D0, "0")]
    [InlineData(Key.D1, "1")]
    [InlineData(Key.D9, "9")]
    [InlineData(Key.OemTilde, "`")]
    [InlineData(Key.OemMinus, "-")]
    [InlineData(Key.OemPlus, "=")]
    [InlineData(Key.OemOpenBrackets, "[")]
    [InlineData(Key.OemCloseBrackets, "]")]
    [InlineData(Key.OemPipe, "\\")]
    [InlineData(Key.OemSemicolon, ";")]
    [InlineData(Key.OemQuotes, "'")]
    [InlineData(Key.OemComma, ",")]
    [InlineData(Key.OemPeriod, ".")]
    [InlineData(Key.OemQuestion, "/")]
    public void ToFriendlyString_ReturnsProperString(Key key, string expected)
    {
        key.ToFriendlyString().Should().Be(expected);
    }

    [Theory]
    [InlineData(Key.A)]
    [InlineData(Key.Z)]
    [InlineData(Key.OemTilde)]
    [InlineData(Key.OemMinus)]
    [InlineData(Key.OemPlus)]
    [InlineData(Key.OemOpenBrackets)]
    [InlineData(Key.OemCloseBrackets)]
    [InlineData(Key.OemPipe)]
    [InlineData(Key.OemSemicolon)]
    [InlineData(Key.OemQuotes)]
    [InlineData(Key.OemComma)]
    [InlineData(Key.OemPeriod)]
    [InlineData(Key.OemQuestion)]
    public void IsApplicationKey_ReturnsTrue_ForLettersAndPunctuation(Key key)
    {
        key.IsApplicationKey().Should().BeTrue();
    }

    [Theory]
    [InlineData(Key.None)]
    [InlineData(Key.D0)]
    [InlineData(Key.D9)]
    [InlineData(Key.F1)]
    [InlineData(Key.Space)]
    [InlineData(Key.Return)]
    [InlineData(Key.Oem8)]
    [InlineData(Key.OemBackslash)]
    public void IsApplicationKey_ReturnsFalse_ForNonApplicationKeys(Key key)
    {
        key.IsApplicationKey().Should().BeFalse();
    }
}

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
    public void KeyExtensions_ToFriendlyString_ReturnsProperString(Key key, string expected)
    {
        key.ToFriendlyString().Should().Be(expected);
    }
}

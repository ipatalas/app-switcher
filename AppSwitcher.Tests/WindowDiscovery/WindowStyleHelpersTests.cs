using AppSwitcher.WindowDiscovery;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.WindowDiscovery;

public class WindowStyleHelpersTests
{
    [Theory]
    [InlineData((uint)WindowStyleEx.WS_EX_TOOLWINDOW, "WS_EX_TOOLWINDOW")]
    [InlineData((uint)WindowStyleEx.WS_EX_TOPMOST, "WS_EX_TOPMOST")]
    [InlineData((uint)WindowStyleEx.WS_EX_NOACTIVATE, "WS_EX_NOACTIVATE")]
    [InlineData((uint)WindowStyleEx.WS_EX_APPWINDOW, "WS_EX_APPWINDOW")]
    [InlineData((uint)WindowStyleEx.WS_EX_LAYERED, "WS_EX_LAYERED")]
    public void GetString_IncludesFlagName_WhenFlagIsSet(uint styleValue, string expectedFlagName)
    {
        var result = WindowStyleHelpers.GetString((WindowStyleEx)styleValue);

        result.Should().Contain(expectedFlagName);
    }

    [Fact]
    public void GetString_DoesNotIncludeUnsetFlagName_WhenFlagIsNotSet()
    {
        // WS_EX_LAYERED (0x00080000) is not a subset of WS_EX_TOPMOST (0x00000008)
        var result = WindowStyleHelpers.GetString(WindowStyleEx.WS_EX_TOPMOST);

        result.Should().NotContain("WS_EX_LAYERED");
    }

    [Fact]
    public void GetString_ContainsBothFlagNames_WhenMultipleFlagsAreSet()
    {
        var style = WindowStyleEx.WS_EX_TOOLWINDOW | WindowStyleEx.WS_EX_NOACTIVATE;

        var result = WindowStyleHelpers.GetString(style);

        result.Should().Contain("WS_EX_TOOLWINDOW").And.Contain("WS_EX_NOACTIVATE");
    }

    [Fact]
    public void GetString_UsesPipeSeparator_WhenMultipleFlagsAreSet()
    {
        var style = WindowStyleEx.WS_EX_TOOLWINDOW | WindowStyleEx.WS_EX_NOACTIVATE;

        var result = WindowStyleHelpers.GetString(style);

        result.Should().Contain(" | ");
    }

    [Fact]
    public void GetString_AlwaysIncludesZeroValueFlagName_BecauseHasFlagAlwaysTrueForZero()
    {
        // WS_EX_RIGHTSCROLLBAR = 0x0 is always "set" because Enum.HasFlag(0) returns true for any value.
        // Enum.GetValues deduplicates same-value members; WS_EX_RIGHTSCROLLBAR is the canonical zero representative.
        var result = WindowStyleHelpers.GetString(WindowStyleEx.WS_EX_TOPMOST);

        result.Should().Contain("WS_EX_RIGHTSCROLLBAR");
    }
}
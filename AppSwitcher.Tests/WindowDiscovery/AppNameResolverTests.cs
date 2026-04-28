using AppSwitcher.Stats;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using NSubstitute;
using System.Windows.Input;
using Xunit;

namespace AppSwitcher.Tests.WindowDiscovery;

public class AppNameResolverTests
{
    private readonly IAppRegistryCache _appRegistryCache = Substitute.For<IAppRegistryCache>();
    private readonly AppNameResolver _sut;

    public AppNameResolverTests()
    {
        _sut = new AppNameResolver(_appRegistryCache);
    }

    [Theory]
    [InlineData("spotify", Key.S)]
    [InlineData("Paint", Key.P)]
    [InlineData("edge", Key.E)]
    [InlineData("WindowsTerminal", Key.W)]
    public void GetDynamicKey_ReturnsExpectedKey_ForFirstLetterOfDisplayName(string displayName, Key expectedKey)
    {
        _appRegistryCache.GetDisplayName(Arg.Any<string>(), Arg.Any<string>()).Returns(displayName);

        var result = _sut.GetDynamicKey("app.exe", @"C:\fake\app.exe");

        result.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("1password")]
    [InlineData("9lives")]
    [InlineData("_tool")]
    public void GetDynamicKey_ReturnsNull_WhenDisplayNameStartsWithNonLetter(string displayName)
    {
        _appRegistryCache.GetDisplayName(Arg.Any<string>(), Arg.Any<string>()).Returns(displayName);

        var result = _sut.GetDynamicKey("app.exe", @"C:\fake\app.exe");

        result.Should().BeNull();
    }

    [Fact]
    public void GetDynamicKey_PassesProcessNameAndPath_ToRegistryCache()
    {
        const string processPath = @"C:\apps\spotify.exe";
        _appRegistryCache.GetDisplayName(Arg.Any<string>(), Arg.Any<string>()).Returns("spotify");

        _sut.GetDynamicKey("spotify.exe", processPath);

        _appRegistryCache.Received(1).GetDisplayName("spotify.exe", processPath);
    }
}

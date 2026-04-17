using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using System.Windows.Input;
using Xunit;

namespace AppSwitcher.Tests.WindowDiscovery;

public class AppNameResolverTests
{
    [Theory]
    [InlineData("mspaint", "Microsoft Corporation", "paint")]
    [InlineData("msedge", "Microsoft Corporation", "edge")]
    [InlineData("MSPAINT", "Microsoft Corporation", "PAINT")]
    [InlineData("msstore", "Microsoft Corporation", "store")]
    public void ResolveDisplayName_StripsMsPrefix_WhenCompanyIsMicrosoft(
        string processFilename, string companyName, string expected)
    {
        var result = AppNameResolver.ResolveDisplayName(companyName, processFilename);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("WindowsTerminal", "Microsoft Corporation", "WindowsTerminal")]
    [InlineData("POWERPNT", "Microsoft Corporation", "POWERPNT")]
    [InlineData("EXCEL", "Microsoft Corporation", "EXCEL")]
    [InlineData("ONENOTE", "Microsoft Corporation", "ONENOTE")]
    public void ResolveDisplayName_DoesNotStrip_WhenFilenameDoesNotStartWithMs(
        string processFilename, string companyName, string expected)
    {
        var result = AppNameResolver.ResolveDisplayName(companyName, processFilename);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("spotify", "Spotify AB", "spotify")]
    [InlineData("rider64", "JetBrains s.r.o.", "rider64")]
    [InlineData("notepad++", "Don HO don.h@free.fr", "notepad++")]
    [InlineData("Obsidian", "Obsidian", "Obsidian")]
    public void ResolveDisplayName_ReturnsFilenameUnchanged_WhenCompanyIsNotMicrosoft(
        string processFilename, string companyName, string expected)
    {
        var result = AppNameResolver.ResolveDisplayName(companyName, processFilename);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("spotify", null, "spotify")]
    [InlineData("baretail", null, "baretail")]
    public void ResolveDisplayName_ReturnsFilenameUnchanged_WhenCompanyIsNull(
        string processFilename, string? companyName, string expected)
    {
        var result = AppNameResolver.ResolveDisplayName(companyName, processFilename);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ResolveDisplayName_ReturnsEmpty_WhenFilenameIsEmpty(string processFilename)
    {
        var result = AppNameResolver.ResolveDisplayName("Microsoft Corporation", processFilename);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("mspaint", "Microsoft Corporation", 'P')]
    [InlineData("msedge", "Microsoft Corporation", 'E')]
    [InlineData("spotify", "Spotify AB", 'S')]
    [InlineData("WindowsTerminal", "Microsoft Corporation", 'W')]
    [InlineData("POWERPNT", "Microsoft Corporation", 'P')]
    [InlineData("EXCEL", "Microsoft Corporation", 'E')]
    [InlineData("rider64", "JetBrains s.r.o.", 'R')]
    [InlineData("vivaldi", "Vivaldi Technologies AS", 'V')]
    public void ResolveDisplayName_FirstChar_MapsToExpectedKey(
        string processFilename, string? companyName, char expectedFirstLetter)
    {
        var displayName = AppNameResolver.ResolveDisplayName(companyName, processFilename);
        var firstChar = char.ToUpperInvariant(displayName[0]);

        firstChar.Should().Be(expectedFirstLetter);
    }
}

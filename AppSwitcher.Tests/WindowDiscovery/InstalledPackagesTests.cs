using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using Xunit;

namespace AppSwitcher.Tests.WindowDiscovery;

public class InstalledPackagesTests
{
    private const string ClaudePackage = @"C:\Program Files\WindowsApps\Claude_2.31226.1.0_x64__pzs8sxrjxfjjc";
    private const string TerminalPackage = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.10351.0_x64__8wekyb3d8bbwe";
    private const string SettingsPackage = @"C:\Windows\ImmersiveControlPanel";
    private const string StartMenuPackage = @"C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy";
    private const string DriveRootPackage = @"D:\DevPackage";

    private readonly InstalledPackages _sut =
        new([ClaudePackage, TerminalPackage, SettingsPackage, StartMenuPackage, DriveRootPackage]);

    [Theory]
    [InlineData(ClaudePackage + @"\app\Claude.exe")]
    [InlineData(TerminalPackage + @"\WindowsTerminal.exe")]
    [InlineData(ClaudePackage + @"\app\resources\bin\helper.exe")]
    [InlineData(@"c:\program files\windowsapps\claude_2.31226.1.0_x64__pzs8sxrjxfjjc\app\claude.exe")]
    [InlineData(SettingsPackage + @"\SystemSettings.exe")]
    [InlineData(StartMenuPackage + @"\StartMenuExperienceHost.exe")]
    [InlineData(DriveRootPackage + @"\bin\app.exe")]
    public void Contains_ReturnsTrue_WhenExecutableIsInsidePackage(string processPath)
    {
        var result = _sut.Contains(processPath);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(@"C:\Program Files\Notepad++\notepad++.exe")]
    [InlineData(@"C:\Program Files\WindowsApps\Claude_2.31226.1.0_x64__pzs8sxrjxfjjcX\app\Claude.exe")]
    [InlineData(@"C:\Program Files\WindowsApps\Claude.exe")]
    [InlineData(@"C:\Windows\System32\notepad.exe")]
    [InlineData(@"C:\Windows\explorer.exe")]
    [InlineData(@"D:\DevPackageOther\app.exe")]
    [InlineData("")]
    public void Contains_ReturnsFalse_WhenExecutableIsOutsideOfPackages(string processPath)
    {
        var result = _sut.Contains(processPath);

        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenInstalledPathHasTrailingSeparator()
    {
        var sut = new InstalledPackages([ClaudePackage + @"\"]);

        var result = sut.Contains($@"{ClaudePackage}\app\Claude.exe");

        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenNoPackagesInstalled()
    {
        var result = InstalledPackages.Empty.Contains($@"{ClaudePackage}\app\Claude.exe");

        result.Should().BeFalse();
    }
}

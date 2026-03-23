using AppSwitcher.Configuration;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.Configuration;

public class ApplicationConfigurationTests
{
    [Theory]
    [InlineData(@"C:\Windows\System32\notepad.exe", "notepad.exe")]
    [InlineData(@"notepad.exe", "notepad.exe")]
    [InlineData(@"C:\Program Files\My App\my.app.exe", "my.app.exe")]
    [InlineData(@"\\server\share\tool.exe", "tool.exe")]
    [InlineData(@"C:\path\to\chrome.exe", "chrome.exe")]
    public void ProcessName_ReturnsFileNameFromProcessPath(string processPath, string expectedName)
    {
        var config = new ApplicationConfiguration(Key.A, processPath, CycleMode.NextApp, false);

        config.ProcessName.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("WindowsTerminal.exe")]
    [InlineData("ms-teams.exe")]
    [InlineData("Calculator.exe")]
    public void ProcessName_ReturnsStoredFilename_ForPackagedApp(string exeFilename)
    {
        var config = new ApplicationConfiguration(
            Key.A, exeFilename, CycleMode.NextApp, false,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");

        config.ProcessName.Should().Be(exeFilename);
    }

    [Fact]
    public void Type_DefaultsToWin32_WhenNotSpecified()
    {
        var config = new ApplicationConfiguration(Key.A, @"C:\Windows\notepad.exe", CycleMode.NextApp, false);

        config.Type.Should().Be(ApplicationType.Win32);
    }

    [Fact]
    public void Aumid_DefaultsToNull_WhenNotSpecified()
    {
        var config = new ApplicationConfiguration(Key.A, @"C:\Windows\notepad.exe", CycleMode.NextApp, false);

        config.Aumid.Should().BeNull();
    }

    [Fact]
    public void Type_IsPackaged_WhenExplicitlySet()
    {
        var config = new ApplicationConfiguration(
            Key.T, "WindowsTerminal.exe", CycleMode.NextApp, false,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");

        config.Type.Should().Be(ApplicationType.Packaged);
        config.Aumid.Should().Be("Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");
    }
}
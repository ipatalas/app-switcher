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
}
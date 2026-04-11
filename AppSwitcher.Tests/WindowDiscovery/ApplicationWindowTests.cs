using AppSwitcher.WindowDiscovery;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.WindowDiscovery;

public class ApplicationWindowTests
{
    // Creates an ApplicationWindow with only the fields relevant to IsValidWindow
    // configurable; all unrelated fields are left at their default values.
    private static ApplicationWindow CreateWindow(
        string title = "Test Window",
        int width = 100,
        int height = 100,
        WindowStyleEx styleEx = WindowStyleEx.WS_EX_APPWINDOW,
        bool isCloaked = false) =>
        new(
            Handle: default,
            Title: title,
            ProcessId: 1,
            ProcessImagePath: "test.exe",
            State: default,
            Position: default,
            Size: new Size(width, height),
            Style: default,
            StyleEx: styleEx,
            IsCloaked: isCloaked,
            NeedsElevation: false);

    [Fact]
    public void IsValidWindow_ReturnsTrue_WhenAllConditionsAreMet()
    {
        CreateWindow().IsValidWindow.Should().BeTrue();
    }

    [Fact]
    public void IsValidWindow_ReturnsFalse_WhenSizeIsEmpty()
    {
        CreateWindow(width: 0, height: 0).IsValidWindow.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidWindow_ReturnsFalse_WhenTitleIsNullOrEmpty(string? title)
    {
        CreateWindow(title: title!).IsValidWindow.Should().BeFalse();
    }

    [Fact]
    public void IsValidWindow_ReturnsFalse_WhenStyleExHasNoActivateFlag()
    {
        CreateWindow(styleEx: WindowStyleEx.WS_EX_NOACTIVATE).IsValidWindow.Should().BeFalse();
    }

    [Fact]
    public void IsValidWindow_ReturnsFalse_WhenStyleExHasToolWindowFlag()
    {
        CreateWindow(styleEx: WindowStyleEx.WS_EX_TOOLWINDOW).IsValidWindow.Should().BeFalse();
    }

    [Fact]
    public void IsValidWindow_ReturnsFalse_WhenWindowIsCloaked()
    {
        CreateWindow(isCloaked: true).IsValidWindow.Should().BeFalse();
    }
}

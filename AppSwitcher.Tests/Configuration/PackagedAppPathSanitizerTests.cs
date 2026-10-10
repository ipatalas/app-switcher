using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Windows.Input;
using Xunit;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Configuration;

public class PackagedAppPathSanitizerTests
{
    private const string ClaudeAumid = "Claude_pzs8sxrjxfjjc!Claude";
    // fictional versions - sanitizer checks File.Exists, so the stale path must not exist on a real machine
    private const string StaleClaudePackage = @"C:\Program Files\WindowsApps\Claude_0.0.1.0_x64__pzs8sxrjxfjjc";
    private const string NewClaudePackage = @"C:\Program Files\WindowsApps\Claude_0.0.2.0_x64__pzs8sxrjxfjjc";

    private readonly IPackagedAppsService _packagedAppsService = Substitute.For<IPackagedAppsService>();
    private readonly PackagedAppPathSanitizer _sut;

    public PackagedAppPathSanitizerTests()
    {
        _packagedAppsService.GetInstalledPathByAumid(ClaudeAumid).Returns(NewClaudePackage);
        _sut = new PackagedAppPathSanitizer(_packagedAppsService, NullLogger<PackagedAppPathSanitizer>.Instance);
    }

    private static AppConfig ConfigWith(params ApplicationConfiguration[] apps) =>
        new(Modifier: Key.RightCtrl,
            Applications: apps,
            PulseBorderEnabled: false,
            Theme: AppThemeSetting.System,
            OverlayEnabled: false,
            OverlayShowDelayMs: 0,
            OverlayKeepOpenWhileModifierHeld: false,
            PeekEnabled: false,
            DynamicModeEnabled: false,
            StatsEnabled: false);

    private static ApplicationConfiguration PackagedApp(string processPath, string aumid = ClaudeAumid) =>
        new(Key.C, processPath, CycleMode.NextApp, StartIfNotRunning: false, ApplicationType.Packaged, aumid);

    [Fact]
    public void Sanitize_KeepsPackageSubdirectory_WhenExecutableIsNotInPackageRoot()
    {
        var config = ConfigWith(PackagedApp($@"{StaleClaudePackage}\app\Claude.exe"));

        var result = _sut.Sanitize(config);

        result!.Applications.Single().ProcessPath.Should().Be($@"{NewClaudePackage}\app\Claude.exe");
    }

    [Fact]
    public void Sanitize_UpdatesPath_WhenExecutableIsInPackageRoot()
    {
        var config = ConfigWith(PackagedApp($@"{StaleClaudePackage}\Claude.exe"));

        var result = _sut.Sanitize(config);

        result!.Applications.Single().ProcessPath.Should().Be($@"{NewClaudePackage}\Claude.exe");
    }

    [Fact]
    public void Sanitize_FallsBackToExecutableName_WhenStalePathIsUnderDifferentRoot()
    {
        var config = ConfigWith(PackagedApp(@"D:\WindowsApps\Claude_0.0.1.0_x64__pzs8sxrjxfjjc\app\Claude.exe"));

        var result = _sut.Sanitize(config);

        result!.Applications.Single().ProcessPath.Should().Be($@"{NewClaudePackage}\Claude.exe");
    }

    [Fact]
    public void Sanitize_ReturnsNull_WhenPackageCannotBeResolved()
    {
        _packagedAppsService.GetInstalledPathByAumid(ClaudeAumid).Returns((string?)null);
        var config = ConfigWith(PackagedApp($@"{StaleClaudePackage}\app\Claude.exe"));

        var result = _sut.Sanitize(config);

        result.Should().BeNull();
    }

    [Fact]
    public void Sanitize_ReturnsNull_WhenNoPackagedApps()
    {
        var config = ConfigWith(new ApplicationConfiguration(Key.N, @"C:\missing\notepad.exe", CycleMode.NextApp,
            StartIfNotRunning: false));

        var result = _sut.Sanitize(config);

        result.Should().BeNull();
    }
}
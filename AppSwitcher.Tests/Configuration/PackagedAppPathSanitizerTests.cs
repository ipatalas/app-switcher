using AppSwitcher.Configuration;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.IO;
using System.Windows.Input;
using Xunit;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Configuration;

public class PackagedAppPathSanitizerTests : IDisposable
{
    private const string ClaudePublisherId = "pzs8sxrjxfjjc";
    private const string ClaudeFamilyName = $"Claude_{ClaudePublisherId}";
    private const string ClaudeAumid = $"Claude_{ClaudePublisherId}!Claude";
    // fictional versions - sanitizer checks File.Exists, so the stale path must not exist on a real machine
    private const string StaleClaudePackage = $@"C:\Program Files\WindowsApps\Claude_0.0.1.0_x64__{ClaudePublisherId}";
    private const string NewClaudePackage = $@"C:\Program Files\WindowsApps\Claude_0.0.2.0_x64__{ClaudePublisherId}";

    private readonly IPackagedAppsService _packagedAppsService = Substitute.For<IPackagedAppsService>();
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"AppSwitcherTests_{Guid.NewGuid():N}");
    private readonly PackagedAppPathSanitizer _sut;

    public PackagedAppPathSanitizerTests()
    {
        _packagedAppsService.GetInstalledPathByAumid(ClaudeAumid).Returns(NewClaudePackage);
        _packagedAppsService.GetByPackageFamilyName(ClaudeFamilyName).Returns(new PackagedAppInfo(ClaudeAumid, null));
        _sut = new PackagedAppPathSanitizer(_packagedAppsService, NullLogger<PackagedAppPathSanitizer>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
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

    private static ApplicationConfiguration Win32App(string processPath) =>
        new(Key.C, processPath, CycleMode.NextApp, StartIfNotRunning: false);

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

    // ── Win32 -> Packaged reclassification ────────────────────────────────────

    [Fact]
    public void Sanitize_ConvertsToPackagedAndFixesPath_WhenWin32AppHasStalePackagePath()
    {
        var config = ConfigWith(Win32App($@"{StaleClaudePackage}\app\claude.exe"));

        var result = _sut.Sanitize(config);

        result!.Applications.Single().Should().Be(
            PackagedApp($@"{NewClaudePackage}\app\claude.exe"));
    }

    [Fact]
    public void Sanitize_ConvertsToPackagedAndKeepsPath_WhenWin32AppPathExists()
    {
        var processPath = Path.Combine(_tempDirectory, $"Claude_0.0.2.0_x64__{ClaudePublisherId}", "app", "claude.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(processPath)!);
        File.WriteAllText(processPath, string.Empty);
        var config = ConfigWith(Win32App(processPath));

        var result = _sut.Sanitize(config);

        result!.Applications.Single().Should().Be(PackagedApp(processPath));
    }

    [Fact]
    public void Sanitize_ReturnsNull_WhenWin32AppPackageIsNotInstalled()
    {
        _packagedAppsService.GetByPackageFamilyName(ClaudeFamilyName).Returns((PackagedAppInfo?)null);
        var config = ConfigWith(Win32App($@"{StaleClaudePackage}\app\claude.exe"));

        var result = _sut.Sanitize(config);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(@"C:\Program Files\Notepad++\notepad++.exe")]
    [InlineData(@"C:\Program Files\WindowsApps\Claude.exe")]
    [InlineData(@"C:\Tools\my_tool_v1_x64_build\tool.exe")]
    public void Sanitize_DoesNotLookUpPackage_WhenWin32AppIsNotInPackageDirectory(string processPath)
    {
        var config = ConfigWith(Win32App(processPath));

        _sut.Sanitize(config);

        _packagedAppsService.DidNotReceive().GetByPackageFamilyName(Arg.Any<string>());
    }
}

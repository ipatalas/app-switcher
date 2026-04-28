using System.IO;
using AppSwitcher.Configuration;
using AppSwitcher.Input;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using System.Windows.Input;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AppSwitcher.Tests.Input;

public class DynamicModeServiceTests
{
    // Fake process paths — key assignment is configured explicitly on the mock
    private const string SpotifyPath = @"C:\fake1\spotify.exe";
    private const string PaintPath = @"C:\fake2\paint.exe";
    private const string PowerpointPath = @"C:\fake3\powerpoint.exe";
    private const string TerminalPath = @"C:\fake4\terminal.exe";
    private const string SlackPath = @"C:\fake5\slack.exe";

    private readonly IAppNameResolver _appNameResolver = Substitute.For<IAppNameResolver>();
    private readonly IWindowEnumerator _windowEnumerator = Substitute.For<IWindowEnumerator>();
    private readonly IPackagedAppsService _packagedAppsService = Substitute.For<IPackagedAppsService>();
    private readonly DynamicModeService _sut;

    public DynamicModeServiceTests()
    {
        _windowEnumerator.GetCachedWindows().Returns([]);
        _packagedAppsService.GetInstalledPaths().Returns(new HashSet<string>());
        _appNameResolver.GetDynamicKey(Path.GetFileName(SpotifyPath), SpotifyPath).Returns(Key.S);
        _appNameResolver.GetDynamicKey(Path.GetFileName(PaintPath), PaintPath).Returns(Key.P);
        _appNameResolver.GetDynamicKey(Path.GetFileName(PowerpointPath), PowerpointPath).Returns(Key.P);
        _appNameResolver.GetDynamicKey(Path.GetFileName(TerminalPath), TerminalPath).Returns(Key.T);
        _appNameResolver.GetDynamicKey(Path.GetFileName(SlackPath), SlackPath).Returns(Key.S);
        _sut = new DynamicModeService(_windowEnumerator, _appNameResolver, NullLogger<DynamicModeService>.Instance, _packagedAppsService);
    }

    [Fact]
    public void GetAppsForKey_ReturnsEmpty_WhenLetterIsStaticallyAssigned()
    {
        _windowEnumerator.GetCachedWindows().Returns([MakeWindow(SpotifyPath)]);
        var staticApps = new[] { MakeStaticApp(Key.S, SlackPath) };

        var result = _sut.GetAppsForKey(Key.S, staticApps);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAppsForKey_ReturnsApps_WhenLetterIsNotStaticallyAssigned()
    {
        _windowEnumerator.GetCachedWindows().Returns([MakeWindow(SpotifyPath)]);

        var result = _sut.GetAppsForKey(Key.S, []);

        result.ShouldHaveProcesses(SpotifyPath);
    }

    [Fact]
    public void GetAppsForKey_ReturnsOnlyAppsMatchingLetter()
    {
        _windowEnumerator.GetCachedWindows().Returns(
        [
            MakeWindow(SpotifyPath),
            MakeWindow(TerminalPath),
            MakeWindow(PaintPath),
        ]);

        var result = _sut.GetAppsForKey(Key.S, []);

        result.ShouldHaveProcesses(SpotifyPath);
    }

    [Fact]
    public void GetAppsForKey_ReturnsMultipleApps_WhenSeveralMatchSameLetter()
    {
        _windowEnumerator.GetCachedWindows().Returns(
        [
            MakeWindow(PaintPath),
            MakeWindow(PowerpointPath),
            MakeWindow(TerminalPath),
        ]);

        var result = _sut.GetAppsForKey(Key.P, []);

        result.ShouldHaveProcesses(PaintPath, PowerpointPath);
    }

    [Fact]
    public void GetAppsForKey_ReturnsEmpty_WhenNoRunningAppMatchesLetter()
    {
        _windowEnumerator.GetCachedWindows().Returns([MakeWindow(SpotifyPath)]);

        var result = _sut.GetAppsForKey(Key.Z, []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAppsForKey_DeduplicatesByProcessPath()
    {
        _windowEnumerator.GetCachedWindows().Returns(
        [
            MakeWindow(SpotifyPath),
            MakeWindow(SpotifyPath), // second window of same process
        ]);

        var result = _sut.GetAppsForKey(Key.S, []);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void GetAppsForKey_ReturnedConfig_HasExpectedProperties()
    {
        _windowEnumerator.GetCachedWindows().Returns([MakeWindow(SpotifyPath)]);

        var result = _sut.GetAppsForKey(Key.S, []);

        result[0].Should().BeEquivalentTo(new
        {
            Key = Key.S,
            ProcessPath = SpotifyPath,
            CycleMode = CycleMode.NextApp,
            StartIfNotRunning = false,
        }, options => options.Including(c => c.Key)
            .Including(c => c.ProcessPath)
            .Including(c => c.CycleMode)
            .Including(c => c.StartIfNotRunning));
    }

    [Fact]
    public void GetAppsForKey_UsesCache_OnSecondCall()
    {
        _windowEnumerator.GetCachedWindows().Returns([MakeWindow(SpotifyPath)]);
        _sut.GetAppsForKey(Key.S, []);

        var result = _sut.GetAppsForKey(Key.S, []);

        result.Should().HaveCount(1);
        _windowEnumerator.Received(2).GetCachedWindows();
        _windowEnumerator.DidNotReceive().GetWindows();
    }

    [Fact]
    public void GetAllDynamicApps_ReturnsAllNonStaticDynamicApps()
    {
        var windows = new List<ApplicationWindow>
        {
            MakeWindow(SpotifyPath),
            MakeWindow(PaintPath),
        };

        var result = _sut.GetAllDynamicApps([], windows);

        result.ShouldHaveProcesses(SpotifyPath, PaintPath);
    }

    [Fact]
    public void GetAllDynamicApps_ExcludesStaticallyAssignedLetters()
    {
        var windows = new List<ApplicationWindow>
        {
            MakeWindow(SpotifyPath),
            MakeWindow(PaintPath),
        };
        var staticApps = new[] { MakeStaticApp(Key.S, SlackPath) };

        var result = _sut.GetAllDynamicApps(staticApps, windows);

        result.ShouldHaveProcesses(PaintPath);
    }

    [Fact]
    public void GetAllDynamicApps_DeduplicatesByProcessPath()
    {
        var windows = new List<ApplicationWindow>
        {
            MakeWindow(SpotifyPath),
            MakeWindow(SpotifyPath), // second window of same process
        };

        var result = _sut.GetAllDynamicApps([], windows);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void GetAllDynamicApps_ReturnsProperTypeBasedOnAppType()
    {
        var windows = new List<ApplicationWindow>
        {
            MakeWindow(SpotifyPath),
            MakeWindow(PaintPath),
            MakeWindow(TerminalPath) // PackagedApp
        };
        _packagedAppsService.GetInstalledPaths().Returns(new HashSet<string>([Path.GetDirectoryName(TerminalPath)!]));

        var result = _sut.GetAllDynamicApps([], windows);

        result.Select(c => c.Type).Should().BeEquivalentTo([
            ApplicationType.Win32, ApplicationType.Win32, ApplicationType.Packaged
        ]);
    }

    [Fact]
    public void GetAllDynamicApps_ReturnsEmpty_WhenNoWindowsProvided()
    {
        var result = _sut.GetAllDynamicApps([], []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllDynamicApps_DoesNotUseInternalCache()
    {
        // Populate the internal cache via GetAppsForKey
        _windowEnumerator.GetCachedWindows().Returns([MakeWindow(SpotifyPath)]);
        _sut.GetAppsForKey(Key.S, []);

        // GetAllDynamicApps uses the provided window list, not the cache
        var freshWindows = new List<ApplicationWindow> { MakeWindow(PaintPath) };
        var result = _sut.GetAllDynamicApps([], freshWindows);

        result.ShouldHaveProcesses(PaintPath);
    }

    private static ApplicationWindow MakeWindow(string processPath) =>
        new(
            Handle: new HWND(1),
            Title: "Test Window",
            ProcessId: 1234,
            ProcessImagePath: processPath,
            State: SHOW_WINDOW_CMD.SW_NORMAL,
            Position: new Point(0, 0),
            Size: new Size(800, 600),
            Style: default,
            StyleEx: default,
            IsCloaked: false,
            NeedsElevation: false);

    private static ApplicationConfiguration MakeStaticApp(Key key, string processPath) =>
        new(key, processPath, CycleMode.NextApp, false);
}

file static class Extensions
{
    public static void ShouldHaveProcesses(this IReadOnlyList<ApplicationConfiguration> configs, params string[] expectedProcessPaths)
    {
        var actualPaths = configs.Select(c => c.ProcessPath).ToList();
        actualPaths.Should().BeEquivalentTo(expectedProcessPaths);
    }
}

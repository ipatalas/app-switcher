using AppSwitcher.Configuration;
using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Windows.Input;
using Xunit;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Stats;

public class AppRegistryCacheTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly IWindowEnumerator _windowEnumerator = Substitute.For<IWindowEnumerator>();
    private readonly IPackagedAppsService _packagedAppsService = Substitute.For<IPackagedAppsService>();
    private readonly IProcessInspector _processInspector = Substitute.For<IProcessInspector>();
    private readonly AppRegistryCache _sut;

    public AppRegistryCacheTests()
    {
        _windowEnumerator.GetWindows().Returns([]);
        _packagedAppsService.GetInstalledPaths().Returns(new HashSet<string>());
        _sut = new AppRegistryCache(
            _db,
            _windowEnumerator,
            _packagedAppsService,
            _processInspector,
            NullLogger<AppRegistryCache>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static AppConfig EmptyConfig() =>
        new(Modifier: Key.RightCtrl,
            Applications: [],
            PulseBorderEnabled: false,
            Theme: AppThemeSetting.System,
            OverlayEnabled: false,
            OverlayShowDelayMs: 0,
            OverlayKeepOpenWhileModifierHeld: false,
            PeekEnabled: false,
            DynamicModeEnabled: false,
            StatsEnabled: false);

    private static AppConfig ConfigWith(params ApplicationConfiguration[] apps) =>
        EmptyConfig() with { Applications = apps };

    private static ApplicationWindow MakeWindow(string processImagePath) =>
        new(Handle: default,
            Title: "Test Window",
            ProcessId: 0,
            ProcessImagePath: processImagePath,
            State: default,
            Position: default,
            Size: default,
            Style: default,
            StyleEx: default,
            IsCloaked: false,
            NeedsElevation: false);

    private static ApplicationConfiguration AppConfigFor(string processPath) =>
        new(Key.None, processPath, CycleMode.NextApp, StartIfNotRunning: false);

    [Fact]
    public void GetDisplayName_ReturnsFileNameWithoutExtension_WhenNotInCache()
    {
        var result = _sut.GetDisplayName("notepad.exe");

        result.Should().Be("notepad");
    }

    [Fact]
    public void GetDisplayName_IsCaseInsensitive_ForProcessName()
    {
        _db.SeedDatabase("notepad.exe", "Notepad");
        _sut.Prepopulate(EmptyConfig());

        var result = _sut.GetDisplayName("notepad.exe");

        result.Should().Be("Notepad");
    }

    [Fact]
    public void GetDisplayName_UsesCachedValue_WithoutHittingDb_OnSecondCall()
    {
        _db.SeedDatabase("notepad.exe", "Notepad");
        _sut.Prepopulate(EmptyConfig());

        _db.DropCollection(AppRegistryDocument.CollectionName);

        var result = _sut.GetDisplayName("notepad.exe");

        result.Should().Be("Notepad");
    }

    [Fact]
    public void TryAdd_ReturnsFalse_WhenProcessAlreadyInCache()
    {
        _db.SeedDatabase("notepad.exe", "Notepad");
        _sut.Prepopulate(EmptyConfig());

        var result = _sut.TryAdd("notepad.exe", "notepad.exe");

        result.Should().BeFalse();
    }

    [Fact]
    public void TryAdd_PersistsEntryToLiteDb_OnCacheMiss()
    {
        _processInspector.GetProcessDisplayName(Arg.Any<string>()).Returns("Notepad");

        _sut.TryAdd("notepad.exe", "notepad.exe");

        var doc = _db.FindByProcessName("notepad.exe");
        doc.Should().NotBeNull();
        doc.DisplayName.Should().Be("Notepad");
    }

    // ── Prepopulate ───────────────────────────────────────────────────────────

    [Fact]
    public void Prepopulate_SeedsCache_FromLiteDb()
    {
        _db.SeedDatabase("notepad.exe", "Notepad");

        _sut.Prepopulate(EmptyConfig());

        _sut.GetDisplayName("notepad.exe").Should().Be("Notepad");
    }

    [Fact]
    public void Prepopulate_SeedsCache_FromRunningWindows()
    {
        _windowEnumerator.GetWindows().Returns([MakeWindow(@"C:\notepad.exe")]);
        _processInspector.GetProcessDisplayName(@"C:\notepad.exe").Returns("Notepad");

        _sut.Prepopulate(EmptyConfig());

        _sut.GetDisplayName("notepad.exe").Should().Be("Notepad");
    }

    [Fact]
    public void Prepopulate_SeedsCache_FromConfiguredApps()
    {
        var config = ConfigWith(AppConfigFor(@"C:\code.exe"));
        _processInspector.GetProcessDisplayName(@"C:\code.exe").Returns("Visual Studio Code");

        _sut.Prepopulate(config);

        _sut.GetDisplayName("code.exe").Should().Be("Visual Studio Code");
    }

    [Fact]
    public void Prepopulate_PrefersDatabaseEntry_OverRunningWindow()
    {
        _db.SeedDatabase("notepad.exe", "DB Notepad");
        _windowEnumerator.GetWindows().Returns([MakeWindow(@"C:\notepad.exe")]);

        _sut.Prepopulate(EmptyConfig());

        _sut.GetDisplayName("notepad.exe").Should().Be("DB Notepad");
        _processInspector.DidNotReceive().GetProcessDisplayName(Arg.Any<string>());
    }

    [Fact]
    public void Prepopulate_SkipsConfiguredApp_IfAlreadyInCache()
    {
        _db.SeedDatabase("code.exe", "Existing");
        var config = ConfigWith(AppConfigFor(@"C:\code.exe"));

        _sut.Prepopulate(config);

        _sut.GetDisplayName("code.exe").Should().Be("Existing");
        _processInspector.DidNotReceive().GetProcessDisplayName(Arg.Any<string>());
    }

    [Fact]
    public void Prepopulate_PersistsNewWindowEntry_ToLiteDb()
    {
        _windowEnumerator.GetWindows().Returns([MakeWindow(@"C:\notepad.exe")]);
        _processInspector.GetProcessDisplayName(@"C:\notepad.exe").Returns("Notepad");

        _sut.Prepopulate(EmptyConfig());

        var doc = _db.FindByProcessName("notepad.exe");
        doc.Should().NotBeNull();
        doc.DisplayName.Should().Be("Notepad");
    }
}

file static class Extensions
{
    private static ILiteCollection<AppRegistryDocument> Collection(this LiteDatabase db) =>
        db.GetCollection<AppRegistryDocument>(AppRegistryDocument.CollectionName);

    public static AppRegistryDocument? FindByProcessName(this LiteDatabase db, string processName) =>
        db.Collection().FindById(processName);

    public static void SeedDatabase(this LiteDatabase db, string processName, string displayName) => db.Collection()
        .Insert(new AppRegistryDocument { ProcessName = processName, DisplayName = displayName });
}

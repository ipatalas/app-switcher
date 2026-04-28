using AppSwitcher.Configuration;
using AppSwitcher.Configuration.Migrations;
using AppSwitcher.WindowDiscovery;
using LiteDB;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.Core;

namespace AppSwitcher.Tests.Configuration.Migrations;

public class FixPackagedAppsMigrationTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly IPackagedAppsService _packagedAppsService = Substitute.For<IPackagedAppsService>();
    private readonly FixPackagedAppsMigration _sut;
    private int _keyOffset = 0;

    public FixPackagedAppsMigrationTests()
    {
        BsonMapper.Global.EnumAsInteger = true;
        _db = new LiteDatabase(":memory:");
        _sut = new FixPackagedAppsMigration(_packagedAppsService);
    }

    public void Dispose() => _db.Dispose();

    private void SeedSettings(params ApplicationConfigurationDocument[] apps)
    {
        _db.GetCollection<SettingsDocument>("settings").Insert(new SettingsDocument
        {
            Id = 1,
            ModifierIdleTimeoutMs = 1000,
            Modifier = Key.LeftCtrl,
            PulseBorderEnabled = true,
            Theme = AppThemeSetting.Dark,
            Applications = [.. apps]
        });
    }

    private void SeedService(string packageDir, PackagedAppInfo? packageInfo)
    {
        _packagedAppsService
            .GetByInstalledPath(Arg.Is<string>(s => s.StartsWith(packageDir, StringComparison.OrdinalIgnoreCase)),
                Arg.Any<uint?>())
            .Returns(packageInfo);
    }

    private List<ApplicationConfigurationDocument> GetApps() => GetSettings().Applications;

    private SettingsDocument GetSettings() => _db.GetCollection<SettingsDocument>("settings").FindById(1);

    private ApplicationConfigurationDocument MakeApp(string processPath, ApplicationType type, string? aumid = null) =>
        new ApplicationConfigurationDocument
        {
            ProcessPath = processPath,
            Key = Key.A + _keyOffset++,
            Type = type,
            Aumid = aumid
        };

    [Fact]
    public void Version_IsOne()
    {
        _sut.Version.Should().Be(1);
    }

    [Fact]
    public void Up_DoesNotThrow_WhenDatabaseIsEmpty()
    {
        var act = () => _sut.Up(_db);

        act.Should().NotThrow();
    }

    [Fact]
    public void Up_PreservesOtherSettings_AfterMigration()
    {
        const string packageDir = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.0_x64__8wekyb3d8bbwe";
        const string exePath = packageDir + @"\WindowsTerminal.exe";
        SeedService(packageDir, new PackagedAppInfo("Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", null));
        SeedSettings(MakeApp(exePath, ApplicationType.Win32));

        _sut.Up(_db);

        var settings = GetSettings();

        settings.ModifierIdleTimeoutMs.Should().Be(1000);
        settings.Modifier.Should().Be(Key.LeftCtrl);
        settings.PulseBorderEnabled.Should().BeTrue();
        settings.Theme.Should().Be(AppThemeSetting.Dark);
    }

    [Fact]
    public void Up_SetsTypeToPackaged_WhenWin32PathMatchesInstalledPackage()
    {
        const string packageDir = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.0_x64__8wekyb3d8bbwe";
        const string exePath = packageDir + @"\WindowsTerminal.exe";
        const string aumid = "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App";
        SeedService(packageDir, new PackagedAppInfo(aumid, null));
        SeedSettings(MakeApp(exePath, ApplicationType.Win32));

        _sut.Up(_db);

        var app = GetApps()[0];
        app.Type.Should().Be(ApplicationType.Packaged);
        app.Aumid.Should().Be(aumid);
    }

    [Fact]
    public void Up_PreservesOriginalProcessPath_WhenConverting()
    {
        const string packageDir = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.0_x64__8wekyb3d8bbwe";
        const string exePath = packageDir + @"\WindowsTerminal.exe";
        SeedService(packageDir, new PackagedAppInfo("Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", null));
        SeedSettings(MakeApp(exePath, ApplicationType.Win32));

        _sut.Up(_db);

        GetApps()[0].ProcessPath.Should().Be(exePath);
    }

    [Fact]
    public void Up_LeavesApp_WhenWin32PathDoesNotMatchAnyPackage()
    {
        const string exePath = @"C:\Windows\System32\notepad.exe";
        SeedSettings(MakeApp(exePath, ApplicationType.Win32));

        _sut.Up(_db);

        var app = GetApps()[0];
        app.Type.Should().Be(ApplicationType.Win32);
        app.Aumid.Should().BeNull();
    }

    [Fact]
    public void Up_LeavesApp_WhenAlreadyPackaged()
    {
        const string packageDir = @"C:\Program Files\WindowsApps\SomeApp_1.0_x64__xyz";
        const string exePath = packageDir + @"\SomeApp.exe";
        const string aumid = "SomePublisher.SomeApp!App";
        SeedService(packageDir, new PackagedAppInfo(aumid, null));
        SeedSettings(MakeApp(exePath, ApplicationType.Packaged, aumid));

        _sut.Up(_db);

        // Already packaged — migration should not touch it (no second lookup, Aumid unchanged)
        var app = GetApps()[0];
        app.Type.Should().Be(ApplicationType.Packaged);
        app.Aumid.Should().Be(aumid);
    }

    [Fact]
    public void Up_ConvertsOnlyMatchingApps_WhenMixedAppsExist()
    {
        const string packageDir = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.0_x64__8wekyb3d8bbwe";
        const string terminalPath = packageDir + @"\WindowsTerminal.exe";
        const string aumid = "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App";
        SeedService(packageDir, new PackagedAppInfo(aumid, null));

        SeedSettings(apps:
        [
            MakeApp(terminalPath, ApplicationType.Win32),
            MakeApp(@"C:\Windows\notepad.exe", ApplicationType.Win32)
        ]);

        _sut.Up(_db);

        var apps = GetApps();
        apps.Should().SatisfyRespectively(first =>
            {
                first.Type.Should().Be(ApplicationType.Packaged);
                first.Aumid.Should().Be(aumid);
            },
            second => second.Type.Should().Be(ApplicationType.Win32));
    }

    class SettingsDocument
    {
        [BsonId] public int Id { get; set; }

        public int? ModifierIdleTimeoutMs { get; init; }

        public Key Modifier { get; init; }

        public List<ApplicationConfigurationDocument> Applications { get; init; } = [];

        public bool PulseBorderEnabled { get; init; }

        public AppThemeSetting Theme { get; init; }
    }

    class ApplicationConfigurationDocument
    {
        public Key Key { get; init; }

        public string ProcessPath { get; init; } = string.Empty;

        public CycleMode CycleMode { get; init; }

        public bool StartIfNotRunning { get; init; }

        public ApplicationType Type { get; init; } = ApplicationType.Win32;

        public string? Aumid { get; init; }
    }
}

using AppSwitcher.Configuration;
using AppSwitcher.Configuration.Migrations;
using AppSwitcher.Configuration.Storage;
using AppSwitcher.Utils;
using LiteDB;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.Configuration.Migrations;

public class FixPackagedAppsMigrationTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly FakePackagedAppsService _packagedAppsService = new();
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
            Modifier = Key.LeftCtrl,
            Applications = [.. apps]
        });
    }

    private List<ApplicationConfigurationDocument> GetApps() =>
        _db.GetCollection<SettingsDocument>("settings").FindById(1).Applications;

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
    public void Up_SetsTypeToPackaged_WhenWin32PathMatchesInstalledPackage()
    {
        const string packageDir = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.0_x64__8wekyb3d8bbwe";
        const string exePath = packageDir + @"\WindowsTerminal.exe";
        const string aumid = "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App";
        _packagedAppsService.Register(packageDir, new PackagedAppInfo(aumid, null));
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
        _packagedAppsService.Register(packageDir, new PackagedAppInfo("Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", null));
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
        _packagedAppsService.Register(packageDir, new PackagedAppInfo(aumid, null));
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
        _packagedAppsService.Register(packageDir, new PackagedAppInfo(aumid, null));

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

    private sealed class FakePackagedAppsService : IPackagedAppsService
    {
        private readonly Dictionary<string, PackagedAppInfo> _packages = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string installedPath, PackagedAppInfo info) => _packages[installedPath] = info;

        public IReadOnlySet<string> GetInstalledPaths() =>
            _packages.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        public PackagedAppInfo? GetByInstalledPath(string path) =>
            _packages
                .FirstOrDefault(kv => path.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                .Value;

        public PackagedAppInfo? GetByAumid(string aumid) =>
            _packages.Values.FirstOrDefault(p => p.Aumid.Equals(aumid, StringComparison.OrdinalIgnoreCase));
    }
}
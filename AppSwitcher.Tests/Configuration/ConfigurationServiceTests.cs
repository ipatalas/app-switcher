using AppSwitcher.Configuration;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Configuration;

public class ConfigurationServiceTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly ConfigurationService _sut;

    public ConfigurationServiceTests()
    {
        _sut = new ConfigurationService(_db, NullLogger<ConfigurationService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void ReadConfiguration_ReturnsDefaults_WhenNoDatabaseRecordExists()
    {
        var result = _sut.ReadConfiguration();

        result.Should().BeEquivalentTo(new AppConfig(
            ModifierIdleTimeoutMs: null,
            Modifier: Key.RightCtrl,
            Applications: [],
            PulseBorderEnabled: true,
            Theme: AppThemeSetting.System));
    }

    [Fact]
    public void ReadConfiguration_ReturnsStoredConfiguration_AfterWrite()
    {
        var config = new AppConfig(
            3000,
            Key.LeftAlt,
            [new ApplicationConfiguration(Key.N, @"C:\Windows\notepad.exe", CycleMode.Hide, false)],
            false,
            AppThemeSetting.Dark);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.Should().BeEquivalentTo(config);
    }

    [Fact]
    public void WriteConfiguration_Overwrites_WhenCalledTwice()
    {
        var first = new AppConfig(1000, Key.LeftCtrl, [], true, AppThemeSetting.Light);
        var second = new AppConfig(2000, Key.RightAlt, [], false, AppThemeSetting.Dark);

        _sut.WriteConfiguration(first);
        _sut.WriteConfiguration(second);
        var result = _sut.ReadConfiguration();

        result.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void ReadConfiguration_PreservesAllApplicationFields()
    {
        var app = new ApplicationConfiguration(Key.B, @"C:\apps\browser.exe", CycleMode.NextWindow, true);
        var config = new AppConfig(null, Key.RightCtrl, [app], true, AppThemeSetting.System);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.Applications.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(app);
    }

    [Fact]
    public void ReadConfiguration_PreservesMultipleApplications()
    {
        var apps = new[]
        {
            new ApplicationConfiguration(Key.A, @"C:\apps\a.exe", CycleMode.NextApp, false),
            new ApplicationConfiguration(Key.B, @"C:\apps\b.exe", CycleMode.Hide, true),
            new ApplicationConfiguration(Key.C, @"C:\apps\c.exe", CycleMode.NextWindow, false),
        };
        var config = new AppConfig(null, Key.LeftAlt, apps, true, AppThemeSetting.System);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.Applications.Should().BeEquivalentTo(apps);
    }

    [Fact]
    public void ReadConfiguration_PreservesNullModifierIdleTimeout()
    {
        var config = new AppConfig(null, Key.LeftCtrl, [], true, AppThemeSetting.System);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.ModifierIdleTimeoutMs.Should().BeNull();
    }
}
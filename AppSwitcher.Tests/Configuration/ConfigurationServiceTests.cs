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

    private static AppConfig MakeConfig(
        int? modifierIdleTimeoutMs = null,
        Key modifier = Key.RightCtrl,
        ApplicationConfiguration[]? applications = null,
        bool pulseBorderEnabled = true,
        AppThemeSetting theme = AppThemeSetting.System,
        bool overlayEnabled = true,
        int overlayShowDelayMs = 1000,
        bool overlayKeepOpenWhileModifierHeld = true)
    {
        return new AppConfig(
            ModifierIdleTimeoutMs: modifierIdleTimeoutMs,
            Modifier: modifier,
            Applications: applications ?? [],
            PulseBorderEnabled: pulseBorderEnabled,
            Theme: theme,
            OverlayEnabled: overlayEnabled,
            OverlayShowDelayMs: overlayShowDelayMs,
            OverlayKeepOpenWhileModifierHeld: overlayKeepOpenWhileModifierHeld);
    }


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
            ModifierIdleTimeoutMs: 0,
            Modifier: Key.RightCtrl,
            Applications: [],
            PulseBorderEnabled: true,
            Theme: AppThemeSetting.System,
            OverlayEnabled: false,
            OverlayShowDelayMs: 1000,
            OverlayKeepOpenWhileModifierHeld: true));
    }

    [Fact]
    public void ReadConfiguration_ReturnsStoredConfiguration_AfterWrite()
    {
        var app = new ApplicationConfiguration(Key.N, @"C:\Windows\notepad.exe", CycleMode.Hide, false);
        var config = MakeConfig(applications: [app]);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.Should().BeEquivalentTo(config);
    }

    [Fact]
    public void WriteConfiguration_Overwrites_WhenCalledTwice()
    {
        var first = MakeConfig();
        var second = first with { ModifierIdleTimeoutMs = 2000, Modifier = Key.RightAlt, Theme = AppThemeSetting.Dark };

        _sut.WriteConfiguration(first);
        _sut.WriteConfiguration(second);
        var result = _sut.ReadConfiguration();

        result.Should().BeEquivalentTo(second);
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
        var config = MakeConfig(applications: apps);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.Applications.Should().BeEquivalentTo(apps);
    }

    [Fact]
    public void ReadConfiguration_PreservesNullModifierIdleTimeout()
    {
        var config = MakeConfig(modifierIdleTimeoutMs: null);

        _sut.WriteConfiguration(config);
        var result = _sut.ReadConfiguration();

        result.ModifierIdleTimeoutMs.Should().BeNull();
    }
}

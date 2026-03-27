using AppSwitcher.Configuration;
using AppSwitcher.Configuration.Storage;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Configuration.Storage;

public class SettingsDocumentTests
{
    [Fact]
    public void ToConfiguration_MapsAllFields()
    {
        var doc = new SettingsDocument
        {
            Id = 1,
            ModifierIdleTimeoutMs = 3000,
            Modifier = Key.LeftAlt,
            Applications =
            [
                new ApplicationConfigurationDocument
                {
                    Key = Key.N,
                    ProcessPath = @"C:\Windows\notepad.exe",
                    CycleMode = CycleMode.NextApp,
                    StartIfNotRunning = false
                }
            ],
            PulseBorderEnabled = false,
            OverlayEnabled = true,
            OverlayShowDelayMs = 500,
            OverlayKeepOpenWhileModifierHeld = true,
            Theme = AppThemeSetting.Dark
        };

        var result = doc.ToConfiguration();

        result.Should().BeEquivalentTo(new AppConfig(
            ModifierIdleTimeoutMs: 3000,
            Modifier: Key.LeftAlt,
            Applications:
            [
                new ApplicationConfiguration(
                    Key: Key.N,
                    ProcessPath: @"C:\Windows\notepad.exe",
                    CycleMode: CycleMode.NextApp,
                    StartIfNotRunning: false)
            ],
            PulseBorderEnabled: false,
            Theme: AppThemeSetting.Dark,
            OverlayEnabled: true,
            OverlayShowDelayMs: 500,
            OverlayKeepOpenWhileModifierHeld: true));
    }

    [Fact]
    public void ToConfiguration_MapsNullModifierIdleTimeout()
    {
        var doc = new SettingsDocument
        {
            Modifier = Key.RightCtrl,
            ModifierIdleTimeoutMs = null,
            Applications = []
        };

        var result = doc.ToConfiguration();

        result.ModifierIdleTimeoutMs.Should().BeNull();
    }

    [Fact]
    public void FromConfiguration_MapsAllFields()
    {
        var config = new AppConfig(
            ModifierIdleTimeoutMs: 5000,
            Modifier: Key.RightCtrl,
            Applications:
            [
                new ApplicationConfiguration(
                    Key: Key.B,
                    ProcessPath: @"C:\apps\browser.exe",
                    CycleMode: CycleMode.Hide,
                    StartIfNotRunning: true)
            ],
            PulseBorderEnabled: true,
            Theme: AppThemeSetting.Light,
            OverlayEnabled: true,
            OverlayShowDelayMs: 500,
            OverlayKeepOpenWhileModifierHeld: true);

        var result = SettingsDocument.FromConfiguration(config: config, id: 42);

        result.Should().BeEquivalentTo(new SettingsDocument
        {
            Id = 42,
            ModifierIdleTimeoutMs = 5000,
            Modifier = Key.RightCtrl,
            Applications =
            [
                new ApplicationConfigurationDocument
                {
                    Key = Key.B,
                    ProcessPath = @"C:\apps\browser.exe",
                    CycleMode = CycleMode.Hide,
                    StartIfNotRunning = true
                }
            ],
            PulseBorderEnabled = true,
            Theme = AppThemeSetting.Light,
            OverlayEnabled = true,
            OverlayShowDelayMs = 500,
            OverlayKeepOpenWhileModifierHeld = true,
        });
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var original = new AppConfig(
            ModifierIdleTimeoutMs: 1500,
            Modifier: Key.LeftShift,
            Applications:
            [
                new ApplicationConfiguration(Key.E, @"C:\apps\editor.exe", CycleMode.NextWindow, false),
                new ApplicationConfiguration(Key.T, @"C:\apps\terminal.exe", CycleMode.NextApp, true)
            ],
            PulseBorderEnabled: true,
            Theme: AppThemeSetting.System,
            OverlayEnabled: true,
            OverlayShowDelayMs: 500,
            OverlayKeepOpenWhileModifierHeld: true);

        var roundTripped = SettingsDocument.FromConfiguration(original, 1).ToConfiguration();

        roundTripped.Should().BeEquivalentTo(original);
    }
}

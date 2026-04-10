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
            OverlayKeepOpenWhileModifierHeld: true,
            PeekEnabled: false));
    }

    [Fact]
    public void FromConfiguration_MapsAllFields()
    {
        var config = new AppConfig(
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
            OverlayKeepOpenWhileModifierHeld: true,
            PeekEnabled: false);

        var result = SettingsDocument.FromConfiguration(config: config, id: 42);

        result.Should().BeEquivalentTo(new SettingsDocument
        {
            Id = 42,
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
            OverlayKeepOpenWhileModifierHeld: true,
            PeekEnabled: false);

        var roundTripped = SettingsDocument.FromConfiguration(original, 1).ToConfiguration();

        roundTripped.Should().BeEquivalentTo(original);
    }
}

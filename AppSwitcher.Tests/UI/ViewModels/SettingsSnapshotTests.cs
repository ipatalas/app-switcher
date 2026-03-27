using AppSwitcher.Configuration;
using AppSwitcher.UI.ViewModels;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.UI.ViewModels;

public class SettingsSnapshotTests
{
    private static SettingsSnapshot Default() => new(
        ModifierIdleTimeoutMs: 1000,
        ModifierKey: Key.LeftCtrl,
        Applications: [new ApplicationShortcutSnapshot(Key.A, "notepad.exe", false, CycleMode.NextApp)],
        PulseBorderEnabled: true,
        Theme: AppThemeSetting.System,
        OverlayEnabled: true,
        OverlayShowDelayMs: 1000,
        OverlayKeepOpenWhileModifierHeld: false);

    [Fact]
    public void Equals_ReturnsTrue_WhenAllFieldsAreEqual()
    {
        var a = Default();
        var b = Default();

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOtherIsNull()
    {
        Default().Should().NotBe(null);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenModifierIdleTimeoutMsDiffers()
    {
        var a = Default();
        var b = a with { ModifierIdleTimeoutMs = 9999 };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenModifierKeyDiffers()
    {
        var a = Default();
        var b = a with { ModifierKey = Key.RightAlt };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenPulseBorderEnabledDiffers()
    {
        var a = Default();
        var b = a with { PulseBorderEnabled = false };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenThemeDiffers()
    {
        var a = Default();
        var b = a with { Theme = AppThemeSetting.Dark };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOverlayEnabledDiffers()
    {
        var a = Default();
        var b = a with { OverlayEnabled = false };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOverlayShowDelayMsDiffers()
    {
        var a = Default();
        var b = a with { OverlayShowDelayMs = 500 };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOverlayKeepOpenWhileModifierHeldDiffers()
    {
        var a = Default();
        var b = a with { OverlayKeepOpenWhileModifierHeld = true };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenApplicationsCountDiffers()
    {
        var a = Default();
        var b = a with { Applications = [] };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenApplicationsContentDiffers()
    {
        var a = Default();
        var b = a with
        {
            Applications = [new ApplicationShortcutSnapshot(Key.B, "notepad.exe", false, CycleMode.NextApp)]
        };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenApplicationsOrderDiffers()
    {
        // SequenceEqual is order-sensitive: same items in a different order are not equal
        ApplicationShortcutSnapshot snap1 = new(Key.A, "a.exe", false, CycleMode.NextApp);
        ApplicationShortcutSnapshot snap2 = new(Key.B, "b.exe", false, CycleMode.Hide);

        var a = Default() with { Applications = [snap1, snap2] };
        var b = Default() with { Applications = [snap2, snap1] };

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenApplicationsListIsEmpty()
    {
        var a = Default() with { Applications = [] };
        var b = Default() with { Applications = [] };

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenModifierIdleTimeoutMsIsNullInBoth()
    {
        var a = Default() with { ModifierIdleTimeoutMs = null };
        var b = Default() with { ModifierIdleTimeoutMs = null };

        a.Should().Be(b);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualSnapshots()
    {
        var a = Default();
        var b = Default();

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // --- ApplicationShortcutSnapshot Type/Aumid equality ---

    [Fact]
    public void ApplicationShortcutSnapshot_Equals_ReturnsTrue_WhenTypeAndAumidMatch()
    {
        var a = new ApplicationShortcutSnapshot(Key.T, "WindowsTerminal.exe", false, CycleMode.NextApp,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");
        var b = new ApplicationShortcutSnapshot(Key.T, "WindowsTerminal.exe", false, CycleMode.NextApp,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");

        a.Should().Be(b);
    }

    [Fact]
    public void ApplicationShortcutSnapshot_Equals_ReturnsFalse_WhenTypeDiffers()
    {
        var a = new ApplicationShortcutSnapshot(Key.A, "notepad.exe", false, CycleMode.NextApp,
            ApplicationType.Win32);
        var b = new ApplicationShortcutSnapshot(Key.A, "notepad.exe", false, CycleMode.NextApp,
            ApplicationType.Packaged, "Some.App_xyz!App");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ApplicationShortcutSnapshot_Equals_ReturnsFalse_WhenAumidDiffers()
    {
        var a = new ApplicationShortcutSnapshot(Key.T, "WindowsTerminal.exe", false, CycleMode.NextApp,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");
        var b = new ApplicationShortcutSnapshot(Key.T, "WindowsTerminal.exe", false, CycleMode.NextApp,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!DifferentApp");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ApplicationShortcutSnapshot_Type_DefaultsToWin32()
    {
        var snap = new ApplicationShortcutSnapshot(Key.A, "notepad.exe", false, CycleMode.NextApp);

        snap.Type.Should().Be(ApplicationType.Win32);
        snap.Aumid.Should().BeNull();
    }

    [Fact]
    public void SettingsSnapshot_Equals_ReturnsFalse_WhenApplicationTypesDiffer()
    {
        var win32App = new ApplicationShortcutSnapshot(Key.T, "WindowsTerminal.exe", false, CycleMode.NextApp,
            ApplicationType.Win32);
        var packagedApp = new ApplicationShortcutSnapshot(Key.T, "WindowsTerminal.exe", false, CycleMode.NextApp,
            ApplicationType.Packaged, "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");

        var a = Default() with { Applications = [win32App] };
        var b = Default() with { Applications = [packagedApp] };

        a.Should().NotBe(b);
    }
}
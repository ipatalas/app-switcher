using AppSwitcher.Configuration;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels;

internal sealed record SettingsSnapshot(
    int? ModifierIdleTimeoutMs,
    Key ModifierKey,
    IReadOnlyList<ApplicationShortcutSnapshot> Applications,
    bool PulseBorderEnabled,
    AppThemeSetting Theme,
    bool OverlayEnabled,
    int OverlayShowDelayMs,
    bool OverlayKeepOpenWhileModifierHeld)
{
    public bool Equals(SettingsSnapshot? other)
    {
        if (other is null)
        {
            return false;
        }

        return ModifierIdleTimeoutMs == other.ModifierIdleTimeoutMs &&
               ModifierKey == other.ModifierKey &&
               PulseBorderEnabled == other.PulseBorderEnabled &&
               Theme == other.Theme &&
               OverlayEnabled == other.OverlayEnabled &&
               OverlayShowDelayMs == other.OverlayShowDelayMs &&
               OverlayKeepOpenWhileModifierHeld == other.OverlayKeepOpenWhileModifierHeld &&
               Applications.SequenceEqual(other.Applications);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ModifierIdleTimeoutMs, ModifierKey, PulseBorderEnabled, Theme, OverlayEnabled,
            OverlayShowDelayMs, OverlayKeepOpenWhileModifierHeld, Applications.Count);
    }
}

internal sealed record ApplicationShortcutSnapshot(
    Key Key,
    string ProcessName,
    bool StartIfNotRunning,
    CycleMode CycleMode,
    ApplicationType Type = ApplicationType.Win32,
    string? Aumid = null);
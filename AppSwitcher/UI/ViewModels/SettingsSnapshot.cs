using AppSwitcher.Configuration;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels;

internal sealed record SettingsSnapshot(
    Key ModifierKey,
    IReadOnlyList<ApplicationShortcutSnapshot> Applications,
    bool PulseBorderEnabled,
    AppThemeSetting Theme,
    bool OverlayEnabled,
    int OverlayShowDelayMs,
    bool OverlayKeepOpenWhileModifierHeld,
    bool PeekEnabled,
    bool DynamicModeEnabled,
    bool StatsEnabled)
{
    // This is needed because regular record equality would do only reference comparison for Applications collection
    public bool Equals(SettingsSnapshot? other)
    {
        if (other is null)
        {
            return false;
        }

        return ModifierKey == other.ModifierKey &&
               PulseBorderEnabled == other.PulseBorderEnabled &&
               Theme == other.Theme &&
               OverlayEnabled == other.OverlayEnabled &&
               OverlayShowDelayMs == other.OverlayShowDelayMs &&
               OverlayKeepOpenWhileModifierHeld == other.OverlayKeepOpenWhileModifierHeld &&
               PeekEnabled == other.PeekEnabled &&
               DynamicModeEnabled == other.DynamicModeEnabled &&
               StatsEnabled == other.StatsEnabled &&
               Applications.SequenceEqual(other.Applications);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ModifierKey);
        hash.Add(PulseBorderEnabled);
        hash.Add(Theme);
        hash.Add(OverlayEnabled);
        hash.Add(OverlayShowDelayMs);
        hash.Add(OverlayKeepOpenWhileModifierHeld);
        hash.Add(PeekEnabled);
        hash.Add(DynamicModeEnabled);
        hash.Add(StatsEnabled);
        return hash.ToHashCode();
    }
}

internal sealed record ApplicationShortcutSnapshot(
    Key Key,
    string ProcessPath,
    bool StartIfNotRunning,
    CycleMode CycleMode,
    ApplicationType Type = ApplicationType.Win32,
    string? Aumid = null);
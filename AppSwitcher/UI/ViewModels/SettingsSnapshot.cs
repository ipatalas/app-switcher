using AppSwitcher.Configuration;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels;

internal sealed record SettingsSnapshot(Key ModifierKey, IReadOnlyList<ApplicationShortcutSnapshot> Applications)
{
    public bool Equals(SettingsSnapshot? other)
    {
        if (other is null)
        {
            return false;
        }

        return ModifierKey == other.ModifierKey &&
               Applications.SequenceEqual(other.Applications);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ModifierKey, Applications.Count);
    }
}

internal sealed record ApplicationShortcutSnapshot(Key Key, string ProcessName, bool StartIfNotRunning, CycleMode CycleMode);
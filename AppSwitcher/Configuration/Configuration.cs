using System.Diagnostics;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal enum CycleMode
{
    Default = 0,
    Hide,
    NextWindow
}

internal record Configuration(Key Modifier, IReadOnlyList<ApplicationConfiguration> Applications);

[DebuggerDisplay("{Key} -> {Process} (CycleMode: {CycleMode})")]
internal record ApplicationConfiguration(Key Key, string Process, CycleMode CycleMode)
{
    public string NormalizedProcessName => Process.EndsWith(".exe") ? Process : $"{Process}.exe";
}

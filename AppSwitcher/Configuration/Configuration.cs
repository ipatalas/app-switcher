using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal enum CycleMode
{
    Default = 0,
    Hide,
    NextWindow
}

internal record Configuration(
    int? ModifierIdleTimeoutMs,
    Key Modifier,
    IReadOnlyList<ApplicationConfiguration> Applications);

[DebuggerDisplay("{Key} -> {Process} (CycleMode: {CycleMode}, StartProcess: {StartIfNotRunning})")]
internal record ApplicationConfiguration(Key Key, string Process, CycleMode CycleMode, bool StartIfNotRunning)
{
    public string NormalizedProcessName => Process.EndsWith(".exe") ? Process : $"{Process}.exe";

    public string ProcessName => Path.GetFileName(NormalizedProcessName);

    public bool HasFullProcessPath => Path.IsPathRooted(Process);
}

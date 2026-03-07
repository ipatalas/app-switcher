using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

public enum CycleMode
{
    NextApp = 0,
    Hide,
    NextWindow
}

internal record Configuration(
    int? ModifierIdleTimeoutMs,
    Key Modifier,
    IReadOnlyList<ApplicationConfiguration> Applications);

[DebuggerDisplay("{Key} -> {ProcessPath} (CycleMode: {CycleMode}, StartProcess: {StartIfNotRunning})")]
public record ApplicationConfiguration(
    Key Key,
    string ProcessPath,
    CycleMode CycleMode,
    bool StartIfNotRunning)
{
    public string ProcessName => Path.GetFileName(ProcessPath);
}

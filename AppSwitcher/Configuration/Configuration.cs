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
    IReadOnlyList<ApplicationConfiguration> Applications,
    bool PulseBorderEnabled = true,
    AppThemeSetting Theme = AppThemeSetting.System,
    bool OverlayEnabled = true,
    int OverlayShowDelayMs = 1000);

[DebuggerDisplay("{Key} -> {ProcessName} (Type: {Type}, CycleMode: {CycleMode}, StartProcess: {StartIfNotRunning})")]
public record ApplicationConfiguration(
    Key Key,
    string ProcessPath,
    CycleMode CycleMode,
    bool StartIfNotRunning,
    ApplicationType Type = ApplicationType.Win32,
    string? Aumid = null)
{
    public string ProcessName => Path.GetFileName(ProcessPath);
}

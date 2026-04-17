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
    Key Modifier,
    IReadOnlyList<ApplicationConfiguration> Applications,
    bool PulseBorderEnabled,
    AppThemeSetting Theme,
    bool OverlayEnabled,
    int OverlayShowDelayMs,
    bool OverlayKeepOpenWhileModifierHeld,
    bool PeekEnabled,
    bool DynamicModeEnabled);

[DebuggerDisplay("{Key} -> {ProcessName} (Type: {Type}, CycleMode: {CycleMode}, StartProcess: {StartIfNotRunning})")]
public record ApplicationConfiguration(
    Key Key,
    string ProcessPath,
    CycleMode CycleMode,
    bool StartIfNotRunning,
    ApplicationType Type = ApplicationType.Win32,
    string? Aumid = null) : IApplicationConfiguration
{
    public string ProcessName => Path.GetFileName(ProcessPath);
}

public interface IApplicationConfiguration
{
    string ProcessName { get; }
    Key Key { get; }
    string ProcessPath { get; }
    CycleMode CycleMode { get; }
    bool StartIfNotRunning { get; }
    ApplicationType Type { get; }
    string? Aumid { get; }
}
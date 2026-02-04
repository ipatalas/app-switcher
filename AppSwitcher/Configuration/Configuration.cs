using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

public enum CycleMode
{
    Default = 0,
    Hide,
    NextWindow
}

internal record Configuration(
    int? ModifierIdleTimeoutMs,
    Key Modifier,
    IReadOnlyList<ApplicationConfiguration> Applications)
{
    [JsonPropertyName("$schema")]
    [JsonInclude]
    [JsonPropertyOrder(-1)]
#pragma warning disable CA1822 // This property is intentionally an instance property to be included in the JSON output
    private string Schema => "config.schema.json";
#pragma warning restore CA1822
};

[DebuggerDisplay("{Key} -> {Process} (CycleMode: {CycleMode}, StartProcess: {StartIfNotRunning})")]
public record ApplicationConfiguration(
    Key Key,
    string Process,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    CycleMode CycleMode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool StartIfNotRunning)
{
    [JsonIgnore]
    public string NormalizedProcessName => Process.EndsWith(".exe") ? Process : $"{Process}.exe";

    [JsonIgnore]
    public string ProcessName => Path.GetFileName(NormalizedProcessName);

    [JsonIgnore]
    public bool HasFullProcessPath => Path.IsPathRooted(Process);
}

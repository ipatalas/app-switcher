using System.Diagnostics;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal record Configuration(Key Modifier, IReadOnlyList<ApplicationConfiguration> Applications);

[DebuggerDisplay("{Key} -> {Process}")]
internal record ApplicationConfiguration(Key Key, string Process)
{
    public string NormalizedProcessName => Process.EndsWith(".exe") ? Process : $"{Process}.exe";
}

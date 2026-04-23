using AppSwitcher.Configuration;

namespace AppSwitcher.UI.ViewModels.Common;

internal sealed record ApplicationSelectionArgs(
    string ProcessName,
    string ProcessPath,
    uint? ProcessId,
    ApplicationType Type = ApplicationType.Win32);
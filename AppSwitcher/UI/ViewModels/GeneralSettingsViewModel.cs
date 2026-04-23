using AppSwitcher.Configuration;
using AppSwitcher.UI.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AppSwitcher.UI.ViewModels;

internal class GeneralSettingsViewModel(ISettingsState state) : ObservableObject
{
    public ISettingsState State { get; } = state;

    // see https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute -> DWMWA_BORDER_COLOR
    public static bool IsPulseBorderSupported { get; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public static IReadOnlyList<ThemeOption> AvailableThemes { get; } =
    [
        new(AppThemeSetting.System, "System (follow Windows)"),
        new(AppThemeSetting.Dark, "Dark"),
        new(AppThemeSetting.Light, "Light"),
    ];
}

internal record ThemeOption(AppThemeSetting Value, string DisplayName);
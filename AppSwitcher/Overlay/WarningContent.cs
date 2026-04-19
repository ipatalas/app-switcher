using Wpf.Ui.Controls;

namespace AppSwitcher.Overlay;

internal record WarningContent(string Title, string Message, SymbolRegular Icon, string? LearnMoreUrl)
{
    public static WarningContent Elevated { get; } = new(
        "Elevated application",
        "AppSwitcher can no longer detect your keystrokes while an elevated app is in focus.",
        SymbolRegular.Shield24,
        "https://app-switcher.com/elevated-apps/");

    public static WarningContent KeyEventsStealing { get; } = new(
        "Restricted application",
        "AppSwitcher cannot detect your keystrokes while this app is in focus.",
        SymbolRegular.Warning24,
        null);
}
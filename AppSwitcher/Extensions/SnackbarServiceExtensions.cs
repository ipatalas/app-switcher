using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AppSwitcher.Extensions;

public static class SnackbarServiceExtensions
{
    private static readonly TimeSpan SnackbarTimeoutShort = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SnackbarTimeoutLong = TimeSpan.FromSeconds(5);

    public static void ShowLong(
        this ISnackbarService snackbarService,
        string title,
        string message,
        ControlAppearance appearance,
        SymbolRegular symbol)
    {
        snackbarService.Show(title, message, appearance, new SymbolIcon { Symbol = symbol }, SnackbarTimeoutLong);
    }

    public static void ShowShort(
        this ISnackbarService snackbarService,
        string title,
        string message,
        ControlAppearance appearance,
        SymbolRegular symbol)
    {
        snackbarService.Show(title, message, appearance, new SymbolIcon { Symbol = symbol }, SnackbarTimeoutShort);
    }
}
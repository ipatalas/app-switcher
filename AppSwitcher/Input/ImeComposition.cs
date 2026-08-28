using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.Ime;

namespace AppSwitcher.Input;

/// <summary>
/// Chinese IMEs (WeType, Microsoft Pinyin) commit the raw composition string (e.g. pinyin letters)
/// into the app when keyboard focus is yanked away while a composition is in progress. Cancelling
/// the composition before switching windows leaves the input field clean instead.
/// </summary>
internal sealed record ImeCancelResult(bool ContextAcquired, int CompositionLength, bool CancelAccepted);

internal static class ImeComposition
{
    /// <summary>Cancels an in-progress IME composition in the given window, if any.</summary>
    public static ImeCancelResult CancelActiveComposition(HWND hwnd)
    {
        var himc = PInvoke.ImmGetContext(hwnd);
        if (himc == HIMC.Null)
        {
            return new ImeCancelResult(false, 0, false);
        }

        try
        {
            var length = CompositionLength(himc);
            // CPS_CANCEL is a no-op without an active composition, but TSF-based IMEs may not
            // report the composition length through the IMM bridge - ask for cancel regardless.
            var accepted = PInvoke.ImmNotifyIME(himc, NOTIFY_IME_ACTION.NI_COMPOSITIONSTR, NOTIFY_IME_INDEX.CPS_CANCEL, 0);
            return new ImeCancelResult(true, length, accepted);
        }
        finally
        {
            PInvoke.ImmReleaseContext(hwnd, himc);
        }
    }

    private static unsafe int CompositionLength(HIMC himc) =>
        PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPSTR, null, 0);
}

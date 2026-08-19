using Windows.Win32.Foundation;

namespace AppSwitcher;

internal enum ToggleWindowAction
{
    Activate,
    Hide
}

/// <summary>
/// Pure resolution for <see cref="Configuration.CycleMode.ToggleWindow"/>:
/// prefer the focused matching window, else a remembered handle, else the first match.
/// </summary>
internal static class ToggleWindowTargetResolver
{
    public readonly record struct Result(HWND Handle, ToggleWindowAction Action);

    /// <param name="matchingHandles">Matching windows in enumeration order (first = preferred fallback).</param>
    /// <param name="currentHandle">Foreground window handle, or null if unknown.</param>
    /// <param name="rememberedHandle">Last toggled handle for this app, or null.</param>
    /// <param name="currentIsMinimized">
    /// True when <paramref name="currentHandle"/> is a minimized window.
    /// Minimized windows can remain the foreground HWND; they should be restored, not hidden again.
    /// </param>
    /// <param name="rememberedIsMinimized">
    /// True when <paramref name="rememberedHandle"/> is a minimized window.
    /// After a hide, Windows often focuses a sibling; restore the remembered window instead of retargeting.
    /// </param>
    /// <returns>Null when <paramref name="matchingHandles"/> is empty.</returns>
    public static Result? Resolve(
        IReadOnlyList<HWND> matchingHandles,
        HWND? currentHandle,
        HWND? rememberedHandle,
        bool currentIsMinimized = false,
        bool rememberedIsMinimized = false)
    {
        if (matchingHandles.Count == 0)
        {
            return null;
        }

        var currentIsMatch = Contains(matchingHandles, currentHandle);
        var rememberedIsMatch = Contains(matchingHandles, rememberedHandle);

        // After we hide the remembered window, Windows often focuses a sibling.
        // Restore that minimized window instead of adopting the sibling as the new target.
        HWND target;
        if (rememberedIsMatch && rememberedIsMinimized && currentIsMatch && currentHandle != rememberedHandle)
        {
            target = rememberedHandle!.Value;
        }
        else if (currentIsMatch)
        {
            target = currentHandle!.Value;
        }
        else if (rememberedIsMatch)
        {
            target = rememberedHandle!.Value;
        }
        else
        {
            target = matchingHandles[0];
        }

        var isForegroundTarget = currentHandle is { } current && current == target;
        var action = isForegroundTarget && !currentIsMinimized
            ? ToggleWindowAction.Hide
            : ToggleWindowAction.Activate;

        return new Result(target, action);
    }

    private static bool Contains(IReadOnlyList<HWND> handles, HWND? handle)
    {
        if (handle is not { } h)
        {
            return false;
        }

        for (var i = 0; i < handles.Count; i++)
        {
            if (handles[i] == h)
            {
                return true;
            }
        }

        return false;
    }
}

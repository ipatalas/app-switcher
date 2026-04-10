namespace AppSwitcher.Overlay;

internal class WindowTitleParser
{
    private static readonly string[] Separators = [" - ", " — ", " | ", " : "];

    /// <summary>
    /// Finds a common word-separator suffix shared by all titles, e.g. " - Vivaldi" or " — App".
    /// Returns null when <paramref name="titles"/> has one or fewer entries, or no qualifying suffix is found.
    /// </summary>
    public string? FindCommonSuffix(IReadOnlyList<string> titles)
    {
        if (titles.Count <= 1)
        {
            return null;
        }

        var suffix = titles[0];

        for (var i = 1; i < titles.Count; i++)
        {
            // Keep shortening the suffix until titles[i] ends with it
            while (!titles[i].EndsWith(suffix))
            {
                // Remove the first character of the suffix
                suffix = suffix[1..];
                if (string.IsNullOrEmpty(suffix))
                {
                    return null;
                }
            }
        }

        foreach (var sep in Separators)
        {
            if (suffix.StartsWith(sep, StringComparison.Ordinal) && suffix.Length - sep.Length >= 2)
            {
                return suffix;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes <paramref name="suffix"/> from the end of <paramref name="title"/> and trims trailing whitespace.
    /// Returns <paramref name="title"/> unchanged when <paramref name="suffix"/> is null or not present.
    /// </summary>
    public string StripSuffix(string title, string? suffix) =>
        suffix is not null && title.EndsWith(suffix, StringComparison.Ordinal)
            ? title[..^suffix.Length].TrimEnd()
            : title;
}

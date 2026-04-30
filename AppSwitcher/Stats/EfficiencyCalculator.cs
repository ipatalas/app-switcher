namespace AppSwitcher.Stats;

internal static class EfficiencyCalculator
{
    private const int AltTabBaseOverhead = 100;
    private const int AltTabScanFactor = 140;

    /// <summary>
    /// T_at = Base + (n * Scan)
    /// 380ms for 2 windows but 1500ms for 10 windows
    /// </summary>
    public static int AltTabTimeMs(int windowCount)
        => AltTabBaseOverhead + (windowCount * AltTabScanFactor);

    /// <summary>
    /// Milliseconds saved by using AppSwitcher instead of Alt+Tab. Never negative.
    /// </summary>
    public static int SavedMs(int windowCount, int actualDurationMs)
        => Math.Max(0, AltTabTimeMs(windowCount) - actualDurationMs);
}